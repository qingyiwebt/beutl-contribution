﻿using Avalonia.Input;
using Avalonia.Platform.Storage;

using Beutl.Graphics;
using Beutl.Graphics.Effects;
using Beutl.Models;
using Beutl.ProjectSystem;
using Beutl.Services;
using Beutl.ViewModels;
using Beutl.ViewModels.Dialogs;
using Beutl.Views.Dialogs;

using AvaPoint = Avalonia.Point;

namespace Beutl.Views;

public partial class EditView
{
    // Todo: Refactor
    private async void OnFrameDrop(object? sender, DragEventArgs e)
    {
        int CalculateZIndex(Scene scene)
        {
            TimeSpan frame = scene.CurrentFrame;
            Element[] elements = scene.Children
                .Where(item => item.Start <= frame && frame < item.Range.End)
                .ToArray();
            return elements.Length == 0 ? 0 : elements.Max(v => v.ZIndex) + 1;
        }

        if (DataContext is not EditViewModel viewModel) return;
        Scene scene = viewModel.Scene;
        TimeSpan frame = scene.CurrentFrame;

        AvaPoint position = e.GetPosition(Image);
        double scaleX = Image.Bounds.Size.Width / viewModel.Scene.Width;
        Point scaledPosition = (position / scaleX).ToBtlPoint();
        Point centerePosition = scaledPosition - new Point(scene.Width / 2, scene.Height / 2);

        if (e.Data.Contains(KnownLibraryItemFormats.FilterEffect)
            || e.Data.Contains(KnownLibraryItemFormats.Transform))
        {
            Drawable? drawable = viewModel.Scene.Renderer.HitTest(new((float)scaledPosition.X, (float)scaledPosition.Y));

            if (drawable != null)
            {
                int zindex = (drawable as DrawableDecorator)?.OriginalZIndex ?? drawable.ZIndex;

                Element? element = scene.Children.FirstOrDefault(v =>
                    v.ZIndex == zindex
                    && v.Start <= scene.CurrentFrame
                    && scene.CurrentFrame < v.Range.End);

                if (element != null)
                {
                    viewModel.SelectedObject.Value = element;
                }

                if (e.Data.Get(KnownLibraryItemFormats.FilterEffect) is Type feType
                    && Activator.CreateInstance(feType) is FilterEffect instance)
                {
                    var fe = drawable.FilterEffect;
                    if (fe is FilterEffectGroup feGroup)
                    {
                        feGroup.Children.BeginRecord<FilterEffect>()
                            .Add(instance)
                            .ToCommand()
                            .DoAndRecord(CommandRecorder.Default);
                    }
                    else
                    {
                        // Todo: Groupじゃない場合の処理
                    }
                }
            }
        }
        else if (e.Data.Get(KnownLibraryItemFormats.SourceOperator) is Type type)
        {
            e.Handled = true;

            int zindex = CalculateZIndex(scene);

            if (e.KeyModifiers == KeyModifiers.Control)
            {
                var desc = new ElementDescription(frame, TimeSpan.FromSeconds(5), zindex, InitialOperator: type, Position: centerePosition);
                var dialogViewModel = new AddElementDialogViewModel(scene, desc);
                var dialog = new AddElementDialog
                {
                    DataContext = dialogViewModel
                };
                await dialog.ShowAsync();
            }
            else
            {
                viewModel.AddElement(new ElementDescription(
                    frame, TimeSpan.FromSeconds(5), zindex, InitialOperator: type, Position: centerePosition));
            }
        }
        else if (e.Data.GetFiles()
            ?.Where(v => v is IStorageFile)
            ?.Select(v => v.TryGetLocalPath())
            .FirstOrDefault(v => v != null) is { } fileName)
        {
            int zindex = CalculateZIndex(scene);

            viewModel.AddElement(new ElementDescription(
                frame, TimeSpan.FromSeconds(5), zindex, FileName: fileName, Position: centerePosition));
        }
    }

    private void OnFrameDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(KnownLibraryItemFormats.SourceOperator)
            || e.Data.Contains(KnownLibraryItemFormats.FilterEffect)
            || e.Data.Contains(KnownLibraryItemFormats.Transform)
            || (e.Data.GetFiles()?.Any() ?? false))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
}
