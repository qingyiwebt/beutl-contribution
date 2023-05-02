﻿namespace Beutl.Media;

public interface IPen
{
    IBrush? Brush { get; }

    IReadOnlyList<float>? DashArray { get; }

    float DashOffset { get; }

    float Thickness { get; }

    float MiterLimit { get; }

    StrokeCap StrokeCap { get; }

    StrokeJoin StrokeJoin { get; }

    StrokeAlignment StrokeAlignment { get; }
}

public interface IMutablePen : IPen, IAffectsRender
{
    IPen ToImmutable();
}
