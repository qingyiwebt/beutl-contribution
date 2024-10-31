﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Serialization;

namespace Beutl.Styling;

[ExcludeFromCodeCoverage]
public static class StyleSerializer
{
    public static ISetter? ToSetter(this JsonNode? json, string name, Type targetType, ICoreSerializationContext context)
    {
        var (property, value, animation) = ToTuple(json, name, targetType, context);
        if (property is null)
            return null;

        var helper = (IGenericHelper)typeof(GenericHelper<>)
            .MakeGenericType(property.PropertyType)!
            .GetField("Instance")!
            .GetValue(null)!;

        return helper.InitializeSetter(property, value, animation);
    }

    public static (CoreProperty? property, Optional<object?> value, IAnimation? animation) ToTuple(JsonNode? json, string name, Type targetType, ICoreSerializationContext context)
    {
        JsonNode? animationNode = null;
        JsonNode? valueNode = null;
        Type ownerType = targetType;

        if (json is JsonValue jsonValue)
        {
            animationNode = null;
            valueNode = jsonValue;
        }
        else if (json is JsonObject jobj)
        {
            if (jobj.TryGetPropertyValue("Owner", out JsonNode? ownerNode)
                && ownerNode is JsonValue ownerValue
                && ownerValue.TryGetValue(out string? ownerStr))
            {
                if (TypeFormat.ToType(ownerStr) is { } ownerType1)
                {
                    ownerType = ownerType1;
                }
                else
                {
                    return default;
                }
            }

            valueNode = jobj["Value"];
            // あとで他のJsonNodeに入れるため
            jobj["Value"] = null;

            animationNode = jobj["Animation"];
        }

        CoreProperty? property = PropertyRegistry.GetRegistered(ownerType).FirstOrDefault(x => x.Name == name);

        if (property == null)
            return default;

        Optional<object?> value = null;
        if (valueNode != null)
        {
            // Todo: 互換性維持のために汚くなってる
            var errorNotifier = new RelaySerializationErrorNotifier(context.ErrorNotifier, property.Name);
            var simJson = new JsonObject
            {
                [property.Name] = valueNode
            };
            var innerContext = new JsonSerializationContext(ownerType, errorNotifier, context, simJson);
            using (ThreadLocalSerializationContext.Enter(innerContext))
            {
                value = property.RouteDeserialize(innerContext);
            }
        }

        return (property, value, animationNode?.ToAnimation(property, context));
    }

    public static (string, JsonNode?) ToJson(this ISetter setter, Type targetType, ICoreSerializationContext context)
    {
        return ToJson(
            setter.Property,
            setter.Value,
            setter.Animation,
            targetType,
            context
        );
    }

    public static (string, JsonNode?) ToJson(CoreProperty property, object? value, IAnimation? animation, Type targetType, ICoreSerializationContext context)
    {
        string? owner = null;
        JsonNode? animationNode = null;
        string? name = property.Name;

        if (!targetType.IsAssignableTo(property.OwnerType))
        {
            owner = TypeFormat.ToString(property.OwnerType);
        }

        // Todo: 互換性維持のために汚くなってる
        var simJson = new JsonObject();
        var errorNotifier = new RelaySerializationErrorNotifier(context.ErrorNotifier, name);
        var innerContext = new JsonSerializationContext(targetType, errorNotifier, context, simJson);
        using (ThreadLocalSerializationContext.Enter(innerContext))
        {
            property.RouteSerialize(innerContext, value);
        }
        JsonNode? jsonNode = simJson[name];
        simJson[name] = null;

        if (animation is not null)
        {
            if (animation.Property.Id != property.Id)
            {
                throw new InvalidOperationException("Animation.Property != Property");
            }

            animationNode = animation.ToJson(innerContext);
        }

        if (jsonNode is JsonValue jsonValue
            && owner == null
            && animationNode == null)
        {
            return (name, jsonValue);
        }
        else if (jsonNode == null && owner == null && animationNode == null)
        {
            return (name, null);
        }
        else
        {
            var json = new JsonObject
            {
                ["Value"] = jsonNode
            };

            if (owner != null)
                json["Owner"] = owner;
            if (animationNode != null)
                json["Animation"] = animationNode;

            return (name, json);
        }
    }

    private interface IGenericHelper
    {
        ISetter InitializeSetter(CoreProperty property, object? value, IAnimation? animation);

        ISetter InitializeSetter(CoreProperty property, Optional<object?> value, IAnimation? animation);
    }

    private sealed class GenericHelper<T> : IGenericHelper
    {
        public static readonly GenericHelper<T> Instance = new();

        public ISetter InitializeSetter(CoreProperty property, object? value, IAnimation? animation)
        {
            var setter = new Setter<T>((CoreProperty<T>)property);
            if (value is T t)
            {
                setter.Value = t;
            }

            setter.Animation = animation as IAnimation<T>;
            return setter;
        }

        public ISetter InitializeSetter(CoreProperty property, Optional<object?> value, IAnimation? animation)
        {
            var setter = new Setter<T>((CoreProperty<T>)property);
            if (value.HasValue && value.Value is T t)
            {
                setter.Value = t;
            }

            setter.Animation = animation as IAnimation<T>;
            return setter;
        }
    }
}
