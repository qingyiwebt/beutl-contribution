﻿using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Beutl.Collections;

namespace Beutl.Configuration;

public sealed class ViewConfig : ConfigurationBase
{
    public static readonly CoreProperty<ViewTheme> ThemeProperty;
    public static readonly CoreProperty<CultureInfo> UICultureProperty;
    public static readonly CoreProperty<bool> HidePrimaryPropertiesProperty;
    public static readonly CoreProperty<(int X, int Y)?> WindowPositionProperty;
    public static readonly CoreProperty<(int Width, int Height)?> WindowSizeProperty;
    public static readonly CoreProperty<bool?> IsWindowMaximizedProperty;
    public static readonly CoreProperty<CoreList<string>> PrimaryPropertiesProperty;
    public static readonly CoreProperty<CoreList<string>> RecentFilesProperty;
    public static readonly CoreProperty<CoreList<string>> RecentProjectsProperty;
    private readonly CoreList<string> _primaryProperties = new()
    {
        "AlignmentX", "AlignmentY", "TransformOrigin", "BlendMode"
    };
    private readonly CoreList<string> _recentFiles = new();
    private readonly CoreList<string> _recentProjects = new();

    static ViewConfig()
    {
        ThemeProperty = ConfigureProperty<ViewTheme, ViewConfig>(nameof(Theme))
            .DefaultValue(ViewTheme.Dark)
            .Register();

        UICultureProperty = ConfigureProperty<CultureInfo, ViewConfig>(nameof(UICulture))
            .DefaultValue(CultureInfo.InstalledUICulture)
            .Register();

        HidePrimaryPropertiesProperty = ConfigureProperty<bool, ViewConfig>(nameof(HidePrimaryProperties))
            .DefaultValue(false)
            .Register();

        WindowPositionProperty = ConfigureProperty<(int X, int Y)?, ViewConfig>(nameof(WindowPosition))
            .DefaultValue(null)
            .Register();

        WindowSizeProperty = ConfigureProperty<(int Width, int Height)?, ViewConfig>(nameof(WindowSize))
            .DefaultValue(null)
            .Register();

        IsWindowMaximizedProperty = ConfigureProperty<bool?, ViewConfig>(nameof(IsWindowMaximized))
            .DefaultValue(null)
            .Register();

        PrimaryPropertiesProperty = ConfigureProperty<CoreList<string>, ViewConfig>(nameof(PrimaryProperties))
            .Accessor(o => o.PrimaryProperties, (o, v) => o.PrimaryProperties = v)
            .Register();

        RecentFilesProperty = ConfigureProperty<CoreList<string>, ViewConfig>(nameof(RecentFiles))
            .Accessor(o => o.RecentFiles, (o, v) => o.RecentFiles = v)
            .Register();

        RecentProjectsProperty = ConfigureProperty<CoreList<string>, ViewConfig>(nameof(RecentProjects))
            .Accessor(o => o.RecentProjects, (o, v) => o.RecentProjects = v)
            .Register();
    }

    public ViewConfig()
    {
        _primaryProperties.CollectionChanged += (_, _) => OnChanged();
        _recentFiles.CollectionChanged += (_, _) => OnChanged();
        _recentProjects.CollectionChanged += (_, _) => OnChanged();
    }

    public ViewTheme Theme
    {
        get => GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public CultureInfo UICulture
    {
        get => GetValue(UICultureProperty);
        set => SetValue(UICultureProperty, value);
    }

    public bool HidePrimaryProperties
    {
        get => GetValue(HidePrimaryPropertiesProperty);
        set => SetValue(HidePrimaryPropertiesProperty, value);
    }

    [NotAutoSerialized]
    public (int X, int Y)? WindowPosition
    {
        get => GetValue(WindowPositionProperty);
        set => SetValue(WindowPositionProperty, value);
    }

    [NotAutoSerialized]
    public (int Width, int Height)? WindowSize
    {
        get => GetValue(WindowSizeProperty);
        set => SetValue(WindowSizeProperty, value);
    }

    public bool? IsWindowMaximized
    {
        get => GetValue(IsWindowMaximizedProperty);
        set => SetValue(IsWindowMaximizedProperty, value);
    }

    [NotAutoSerialized()]
    public CoreList<string> PrimaryProperties
    {
        get => _primaryProperties;
        set => _primaryProperties.Replace(value);
    }

    [NotAutoSerialized()]
    public CoreList<string> RecentFiles
    {
        get => _recentFiles;
        set => _recentFiles.Replace(value);
    }

    [NotAutoSerialized()]
    public CoreList<string> RecentProjects
    {
        get => _recentProjects;
        set => _recentProjects.Replace(value);
    }

    public enum ViewTheme
    {
        Light,
        Dark,
        HighContrast,
        System
    }

    public override void ReadFromJson(JsonObject json)
    {
        base.ReadFromJson(json);
        JsonNode? GetNode(string name1, string name2)
        {
            if (json[name1] is JsonNode node1)
                return node1;
            else if (json[name2] is JsonNode node2)
                return node2;
            else
                return null;
        }

        if (GetNode("primary-properties", nameof(PrimaryProperties)) is JsonArray primaryProperties)
        {
            _primaryProperties.Replace(primaryProperties.Select(i => (string?)i).Where(i => i != null).ToArray()!);
        }

        if (GetNode("recent-files", nameof(RecentFiles)) is JsonArray recentFiles)
        {
            _recentFiles.Replace(recentFiles.Select(i => (string?)i).Where(i => i != null && File.Exists(i)).ToArray()!);
        }

        if (GetNode("recent-projects", nameof(RecentProjects)) is JsonArray recentProjects)
        {
            _recentProjects.Replace(recentProjects.Select(i => (string?)i).Where(i => i != null && File.Exists(i)).ToArray()!);
        }

        WindowPosition = null;
        if (json[nameof(WindowPosition)] is JsonObject pos)
        {
            if (pos["X"] is JsonValue xx && xx.TryGetValue(out int x))
            {
                if (pos["Y"] is JsonValue yy && yy.TryGetValue(out int y))
                {
                    WindowPosition = (x, y);
                }
            }
        }

        WindowSize = null;
        if (json[nameof(WindowSize)] is JsonObject size)
        {
            if (size["Width"] is JsonValue ww && ww.TryGetValue(out int w))
            {
                if (size["Height"] is JsonValue hh && hh.TryGetValue(out int h))
                {
                    WindowSize = (w, h);
                }
            }
        }
    }

    public override void WriteToJson(JsonObject json)
    {
        base.WriteToJson(json);
        json[nameof(PrimaryProperties)] = JsonSerializer.SerializeToNode(_primaryProperties, JsonHelper.SerializerOptions);
        json[nameof(RecentFiles)] = JsonSerializer.SerializeToNode(_recentFiles, JsonHelper.SerializerOptions);
        json[nameof(RecentProjects)] = JsonSerializer.SerializeToNode(_recentProjects, JsonHelper.SerializerOptions);

        if (WindowPosition.HasValue)
        {
            json[nameof(WindowPosition)] = new JsonObject()
            {
                ["X"] = WindowPosition.Value.X,
                ["Y"] = WindowPosition.Value.Y,
            };
        }

        if (WindowSize.HasValue)
        {
            json[nameof(WindowSize)] = new JsonObject()
            {
                ["Width"] = WindowSize.Value.Width,
                ["Height"] = WindowSize.Value.Height,
            };
        }
        
    }

    public void UpdateRecentFile(string filename)
    {
        _recentFiles.Remove(filename);
        _recentFiles.Insert(0, filename);
    }

    public void UpdateRecentProject(string filename)
    {
        _recentProjects.Remove(filename);
        _recentProjects.Insert(0, filename);
    }

    public void ResetPrimaryProperties()
    {
        PrimaryProperties.Replace(new[] { "AlignmentX", "AlignmentY", "TransformOrigin", "BlendMode" });
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.PropertyName is "Theme" or "UICulture" or "HidePrimaryProperties")
        {
            OnChanged();
        }
    }
}
