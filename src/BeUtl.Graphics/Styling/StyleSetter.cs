﻿using System.Reactive.Linq;
using System.Reactive;

using BeUtl.Animation;
using BeUtl.Collections;
using BeUtl.Reactive;

namespace BeUtl.Styling;

public class StyleSetter<T> : LightweightObservableBase<Style?>, ISetter
{
    private CoreProperty<T>? _property;
    private Style? _value;

    public StyleSetter()
    {
    }

    public StyleSetter(CoreProperty<T> property, Style? value)
    {
        _property = property;
        Value = value;
    }

    public CoreProperty<T> Property
    {
        get => _property ?? throw new InvalidOperationException();
        set => _property = value;
    }

    public Style? Value
    {
        get => _value;
        set
        {
            if (_value != null)
            {
                _value.Invalidated -= OnInvalidated;
            }
            if (value != null)
            {
                value.Invalidated += OnInvalidated;
            }

            if (_value != value)
            {
                _value = value;
                PublishNext(value);

                Invalidated?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    CoreProperty ISetter.Property => Property;

    object? ISetter.Value => Value;

    IAnimation? ISetter.Animation => throw new InvalidOperationException();

    public event EventHandler? Invalidated;

    public ISetterInstance Instance(IStyleable target)
    {
        if (Value?.TargetType?.IsAssignableTo(typeof(T)) == false)
        {
            throw new InvalidCastException($"Unable to cast object of type {Value?.TargetType} to type {typeof(T)}.");
        }
        return new StyleSetterInstance<T>(this, target);
    }

    public IObservable<Unit> GetObservable()
    {
        return this.Select(i => Unit.Default);
    }

    protected override void Subscribed(IObserver<Style?> observer, bool first)
    {
        observer.OnNext(_value);
    }

    protected override void Initialize()
    {
    }

    protected override void Deinitialize()
    {
    }

    private void OnInvalidated(object? sender, EventArgs e)
    {
        Invalidated?.Invoke(this, EventArgs.Empty);
    }
}
