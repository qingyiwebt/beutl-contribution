﻿using BEditorNext.Media;
using BEditorNext.Media.Pixel;

namespace BEditorNext.Graphics.Effects;

public class ChromaKey : PixelEffect
{
    private Hsv _hsv;

    public Color Color
    {
        get => _hsv.ToColor();
        set => _hsv = new Hsv(value);
    }

    public int SaturationRange { get; set; }

    public int HueRange { get; set; }

    public override void Apply(ref Bgra8888 pixel, in BitmapInfo info, int index)
    {
        var srcHsv = pixel.ToColor().ToHsv();

        if (Math.Abs(_hsv.H - srcHsv.H) < HueRange &&
            Math.Abs(_hsv.S - srcHsv.S) < SaturationRange)
        {
            pixel = default;
        }
    }
}
