﻿using BEditorNext.Media;
using BEditorNext.Media.Pixel;

namespace BEditorNext.Graphics.Effects;

public class ApplyColorMatrix : PixelEffect
{
    public ColorMatrix Matrix { get; set; }

    public override void Apply(ref Bgra8888 pixel, in BitmapInfo info, int index)
    {
        var vec = new ColorVector(pixel.R / 255F, pixel.G / 255F, pixel.B / 255F, pixel.A / 255F);

        vec *= Matrix;

        pixel = new Bgra8888(
            (byte)(vec.R * 255),
            (byte)(vec.G * 255),
            (byte)(vec.B * 255),
            (byte)(vec.A * 255));
    }
}
