﻿using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Beutl.Media;

namespace Beutl.Graphics;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ColorMatrix : IEquatable<ColorMatrix>
{
    public ColorMatrix(
        float m11, float m12, float m13, float m14, float m15,
        float m21, float m22, float m23, float m24, float m25,
        float m31, float m32, float m33, float m34, float m35,
        float m41, float m42, float m43, float m44, float m45)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M14 = m14;
        M15 = m15;

        M21 = m21;
        M22 = m22;
        M23 = m23;
        M24 = m24;
        M25 = m25;

        M31 = m31;
        M32 = m32;
        M33 = m33;
        M34 = m34;
        M35 = m35;

        M41 = m41;
        M42 = m42;
        M43 = m43;
        M44 = m44;
        M45 = m45;
    }

    public static ColorMatrix Identity { get; } =
        new ColorMatrix(
            1F, 0F, 0F, 0F, 0F,
            0F, 1F, 0F, 0F, 0F,
            0F, 0F, 1F, 0F, 0F,
            0F, 0F, 0F, 1F, 0F);

    public bool IsIdentity
    {
        get
        {
            return
                M11 == 1F && M12 == 0F && M13 == 0F && M14 == 0F && M15 == 0F &&
                M21 == 0F && M22 == 1F && M23 == 0F && M24 == 0F && M25 == 0F &&
                M31 == 0F && M32 == 0F && M33 == 1F && M34 == 0F && M35 == 0F &&
                M41 == 0F && M42 == 0F && M43 == 0F && M44 == 1F && M45 == 0F;
        }
    }

    public float M11 { get; }

    public float M12 { get; }

    public float M13 { get; }

    public float M14 { get; }

    public float M15 { get; }

    public float M21 { get; }

    public float M22 { get; }

    public float M23 { get; }

    public float M24 { get; }

    public float M25 { get; }

    public float M31 { get; }

    public float M32 { get; }

    public float M33 { get; }

    public float M34 { get; }

    public float M35 { get; }

    public float M41 { get; }

    public float M42 { get; }

    public float M43 { get; }

    public float M44 { get; }

    public float M45 { get; }

    public static ColorMatrix CreateFromSpan(ReadOnlySpan<float> span)
    {
        if (span.Length < 20)
            throw new ArgumentException("'span'の長さは20以上である必要がある");

        return new ColorMatrix(
            span[0], span[1], span[2], span[3], span[4],
            span[5], span[6], span[7], span[8], span[9],
            span[10], span[11], span[12], span[13], span[14],
            span[15], span[16], span[17], span[18], span[19]);
    }

    public static ColorMatrix CreateSaturate(float s)
    {
        Span<float> span = stackalloc float[20];
        CreateSaturateMatrix(s, span);

        return CreateFromSpan(span);
    }

    public static ColorMatrix CreateHueRotate(float hue)
    {
        Span<float> span = stackalloc float[20];
        CreateHueRotateMatrix(hue, span);

        return CreateFromSpan(span);
    }

    public static ColorMatrix CreateLuminanceToAlpha()
    {
        Span<float> span = stackalloc float[20];
        CreateLuminanceToAlphaMatrix(span);

        return CreateFromSpan(span);
    }

    public float[] ToArray()
    {
        return new float[]
        {
            M11, M12, M13, M14, M15,
            M21, M22, M23, M24, M25,
            M31, M32, M33, M34, M35,
            M41, M42, M43, M44, M45,
        };
    }

    internal float[] ToArrayForSkia()
    {
        return new float[]
        {
            M11, M12, M13, M14, M15 * 255,
            M21, M22, M23, M24, M25 * 255,
            M31, M32, M33, M34, M35 * 255,
            M41, M42, M43, M44, M45 * 255,
        };
    }

    public static bool operator ==(in ColorMatrix value1, in ColorMatrix value2) => value1.Equals(value2);

    public static bool operator !=(in ColorMatrix value1, in ColorMatrix value2) => !value1.Equals(value2);

    public static Color operator *(in ColorMatrix left, Color right)
    {
        float r = right.R / 255f;
        float g = right.G / 255f;
        float b = right.B / 255f;
        float a = right.A / 255f;
        float newR = left.M11 * r + left.M12 * g + left.M13 * b + left.M14 * a + left.M15;
        float newG = left.M21 * r + left.M22 * g + left.M23 * b + left.M24 * a + left.M25;
        float newB = left.M31 * r + left.M32 * g + left.M33 * b + left.M34 * a + left.M35;
        float newA = left.M41 * r + left.M42 * g + left.M43 * b + left.M44 * a + left.M45;

        return Color.FromArgb(
            (byte)MathF.Round(newA * 255),
            (byte)MathF.Round(newR * 255),
            (byte)MathF.Round(newG * 255),
            (byte)MathF.Round(newB * 255));
    }

    public static ColorMatrix operator *(in ColorMatrix left, in ColorMatrix right)
    {
        ref Matrix4x5 leftImpl = ref Unsafe.As<ColorMatrix, Matrix4x5>(ref Unsafe.AsRef(in left));
        ref Matrix4x5 rightImpl = ref Unsafe.As<ColorMatrix, Matrix4x5>(ref Unsafe.AsRef(in right));

        return (leftImpl * rightImpl).AsColorMatrix();
    }

    public override bool Equals(object? obj)
    {
        return obj is ColorMatrix matrix && Equals(matrix);
    }

    public bool Equals(ColorMatrix other) =>
        M11 == other.M11
        && M12 == other.M12
        && M13 == other.M13
        && M14 == other.M14
        && M15 == other.M15
        && M21 == other.M21
        && M22 == other.M22
        && M23 == other.M23
        && M24 == other.M24
        && M25 == other.M25
        && M31 == other.M31
        && M32 == other.M32
        && M33 == other.M33
        && M34 == other.M34
        && M35 == other.M35
        && M41 == other.M41
        && M42 == other.M42
        && M43 == other.M43
        && M44 == other.M44
        && M45 == other.M45;

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(M11);
        hash.Add(M12);
        hash.Add(M13);
        hash.Add(M14);
        hash.Add(M15);
        hash.Add(M21);
        hash.Add(M22);
        hash.Add(M23);
        hash.Add(M24);
        hash.Add(M25);
        hash.Add(M31);
        hash.Add(M32);
        hash.Add(M33);
        hash.Add(M34);
        hash.Add(M35);
        hash.Add(M41);
        hash.Add(M42);
        hash.Add(M43);
        hash.Add(M44);
        hash.Add(M45);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        CultureInfo ci = CultureInfo.CurrentCulture;

        return string.Format(ci, "{{ {{M00:{0} M01:{1} M02:{2} M03:{3} M04:{4}}} {{M10:{5} M11:{6} M12:{7} M13:{8} M14:{9}}} {{M20:{10} M21:{11} M22:{12} M23:{13} M24:{14}}} {{M30:{15} M31:{16} M32:{17} M33:{18} M34:{19}}} }}",
                             M11.ToString(ci), M12.ToString(ci), M13.ToString(ci), M14.ToString(ci), M15.ToString(ci),
                             M21.ToString(ci), M22.ToString(ci), M23.ToString(ci), M24.ToString(ci), M25.ToString(ci),
                             M31.ToString(ci), M32.ToString(ci), M33.ToString(ci), M34.ToString(ci), M35.ToString(ci),
                             M41.ToString(ci), M42.ToString(ci), M43.ToString(ci), M44.ToString(ci), M45.ToString(ci));
    }

    internal static void CreateSaturateMatrix(float s, Span<float> span)
    {
        span[0] = 0.213F + 0.787F * s;
        span[1] = 0.715F - 0.715F * s;
        span[2] = 0.072F - 0.072F * s;
        span[3] = span[4] = 0;
        span[5] = 0.213F - 0.213F * s;
        span[6] = 0.715F + 0.285F * s;
        span[7] = 0.072F - 0.072F * s;
        span[8] = span[9] = 0;
        span[10] = 0.213F - 0.213F * s;
        span[11] = 0.715F - 0.715F * s;
        span[12] = 0.072F + 0.928F * s;
        span[13] = span[14] = 0;
        span[15] = span[16] = span[17] = 0;
        span[18] = 1;
        span[19] = 0;

    }

    internal static void CreateHueRotateMatrix(float hue, Span<float> span)
    {
        float cosHue = MathF.Cos(hue * MathF.PI / 180);
        float sinHue = MathF.Sin(hue * MathF.PI / 180);
        span[0] = 0.213F + cosHue * 0.787F - sinHue * 0.213F;
        span[1] = 0.715F - cosHue * 0.715F - sinHue * 0.715F;
        span[2] = 0.072F - cosHue * 0.072F + sinHue * 0.928F;
        span[3] = span[4] = 0;
        span[5] = 0.213F - cosHue * 0.213F + sinHue * 0.143F;
        span[6] = 0.715F + cosHue * 0.285F + sinHue * 0.140F;
        span[7] = 0.072F - cosHue * 0.072F - sinHue * 0.283F;
        span[8] = span[9] = 0;
        span[10] = 0.213F - cosHue * 0.213F - sinHue * 0.787F;
        span[11] = 0.715F - cosHue * 0.715F + sinHue * 0.715F;
        span[12] = 0.072F + cosHue * 0.928F + sinHue * 0.072F;
        span[13] = span[14] = 0;
        span[15] = span[16] = span[17] = 0;
        span[18] = 1;
        span[19] = 0;
    }

    internal static void CreateLuminanceToAlphaMatrix(Span<float> span)
    {
        span[0] = span[6] = span[12] = span[18] = 1;
        span[15] = 0.2125F;
        span[16] = 0.7154F;
        span[17] = 0.0721F;
    }

    internal static void ToSkiaColorMatrix(Span<float> array)
    {
        array[4] *= 255;
        array[9] *= 255;
        array[14] *= 255;
        array[19] *= 255;
    }

    internal struct Vector5
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
        public float V;

        public Vector5(float x, float y, float z, float w, float v)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            V = v;
        }

        public static Vector5 operator *(in Vector5 left, in Vector5 right)
        {
            return new()
            {
                X = left.X * right.X,
                Y = left.Y * right.Y,
                Z = left.Z * right.Z,
                W = left.W * right.W,
                V = left.V * right.V,
            };
        }

        public static Vector5 operator *(in Vector5 left, float right)
        {
            return new()
            {
                X = left.X * right,
                Y = left.Y * right,
                Z = left.Z * right,
                W = left.W * right,
                V = left.V * right,
            };
        }

        public static Vector5 operator +(in Vector5 left, in Vector5 right)
        {
            return new()
            {
                X = left.X + right.X,
                Y = left.Y + right.Y,
                Z = left.Z + right.Z,
                W = left.W + right.W,
                V = left.V + right.V,
            };
        }
    }

    internal struct Matrix4x5
    {
        public Vector5 X;
        public Vector5 Y;
        public Vector5 Z;
        public Vector5 W;

        public Matrix4x5(Vector5 x, Vector5 y, Vector5 z, Vector5 w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public readonly ColorMatrix AsColorMatrix() => Unsafe.As<Matrix4x5, ColorMatrix>(ref Unsafe.AsRef(in this));

        public static Matrix4x5 operator *(in Matrix4x5 left, float right)
        {
            return new()
            {
                X = left.X * right,
                Y = left.Y * right,
                Z = left.Z * right,
                W = left.W * right
            };
        }

        public static Matrix4x5 operator *(in Matrix4x5 left, in Matrix4x5 right)
        {
            Matrix4x5 result;

            // result.X = Transform(left.X, in right);
            result.X = right.X * left.X.X;
            result.X += right.Y * left.X.Y;
            result.X += right.Z * left.X.Z;
            result.X += right.W * left.X.W;
            result.X.V += left.X.V;

            // result.Y = Transform(left.Y, in right);
            result.Y = right.X * left.Y.X;
            result.Y += right.Y * left.Y.Y;
            result.Y += right.Z * left.Y.Z;
            result.Y += right.W * left.Y.W;
            result.Y.V += left.Y.V;

            // result.Z = Transform(left.Z, in right);
            result.Z = right.X * left.Z.X;
            result.Z += right.Y * left.Z.Y;
            result.Z += right.Z * left.Z.Z;
            result.Z += right.W * left.Z.W;
            result.Z.V += left.Z.V;

            // result.W = Transform(left.W, in right);
            result.W = right.X * left.W.X;
            result.W += right.Y * left.W.Y;
            result.W += right.Z * left.W.Z;
            result.W += right.W * left.W.W;
            result.W.V += left.W.V;

            return result;
        }
    }
}
