﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

using Beutl.Animation;
using Beutl.Framework;
using Beutl.Operation;
using Beutl.Styling;
using Beutl.ViewModels;
using Beutl.ViewModels.Editors;
using Beutl.ViewModels.Tools;
using Beutl.Views.Tools;

namespace Beutl.Views.Editors;

public sealed partial class PropertyEditorMenu : UserControl
{
    public PropertyEditorMenu()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaseEditorViewModel viewModel)
        {
            if (!viewModel.HasAnimation.Value && sender is Button button)
            {
                button.ContextMenu?.Open();
            }
            else if (this.FindLogicalAncestorOfType<EditView>()?.DataContext is EditViewModel editViewModel)
            {
                if (symbolIcon.IsFilled)
                {
                    viewModel.RemoveKeyFrame(editViewModel.Scene.CurrentFrame);
                }
                else
                {
                    viewModel.InsertKeyFrame(editViewModel.Scene.CurrentFrame);
                }
            }
        }
    }

    private void EditAnimation_Click(object? sender, RoutedEventArgs e)
    {
        if (this.FindLogicalAncestorOfType<EditView>()?.DataContext is EditViewModel editViewModel
            && DataContext is BaseEditorViewModel viewModel
            && viewModel.WrappedProperty is IAbstractAnimatableProperty { Animation: IKeyFrameAnimation kfAnimation })
        {
            // 右側のタブを開く
            //AnimationTabViewModel anmViewModel
            //    = editViewModel.FindToolTab<AnimationTabViewModel>()
            //        ?? new AnimationTabViewModel();

            //anmViewModel.Animation.Value = animatableProperty;

            //editViewModel.OpenToolTab(anmViewModel);

            // タイムラインのタブを開く
            var anmTimelineViewModel = new GraphEditorTabViewModel();
            anmTimelineViewModel.SelectedAnimation.Value = new GraphEditorViewModel(editViewModel, kfAnimation);
            editViewModel.OpenToolTab(anmTimelineViewModel);
        }
    }

    private void EditInlineAnimation_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaseEditorViewModel viewModel
            && viewModel.WrappedProperty is IAbstractAnimatableProperty animatableProperty
            && this.FindLogicalAncestorOfType<EditView>()?.DataContext is EditViewModel editViewModel
            && this.FindLogicalAncestorOfType<SourceOperatorsTab>()?.DataContext is SourceOperatorsTabViewModel { Layer.Value: { } layer }
            && editViewModel.FindToolTab<TimelineViewModel>() is { } timeline)
        {
            if (animatableProperty.Animation is not IKeyFrameAnimation)
            {
                Type type = typeof(KeyFrameAnimation<>).MakeGenericType(animatableProperty.Property.PropertyType);
                animatableProperty.Animation = Activator.CreateInstance(type, animatableProperty.Property) as IAnimation;
            }

            //// 右側のタブを開く
            //AnimationTabViewModel anmViewModel
            //    = editViewModel.FindToolTab<AnimationTabViewModel>()
            //        ?? new AnimationTabViewModel();

            //anmViewModel.Animation.Value = animatableProperty;

            //editViewModel.OpenToolTab(anmViewModel);

            // タイムラインのタブを開く
            timeline.AttachInline(animatableProperty, layer);
        }
    }

    private void DeleteSetter_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BaseEditorViewModel viewModel
            && this.FindLogicalAncestorOfType<StyleEditor>()?.DataContext is StyleEditorViewModel parentViewModel
            && viewModel.WrappedProperty is IStylingSetterPropertyImpl wrapper
            && parentViewModel.Style.Value is Style style)
        {
            style.Setters.BeginRecord<ISetter>()
                .Remove(wrapper.Setter)
                .ToCommand()
                .DoAndRecord(CommandRecorder.Default);
        }
    }
}
