using Ryujinx.Common;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.GAL
{
    public readonly struct TextureCreateInfo : IEquatable<TextureCreateInfo>
    {
        public int Width         { get; }
        public int Height        { get; }
        public int Depth         { get; }
        public int Levels        { get; }
        public int Samples       { get; }
        public int BlockWidth    { get; }
        public int BlockHeight   { get; }
        public int BytesPerPixel { get; }

        public bool IsCompressed => (BlockWidth | BlockHeight) != 1;

        public Format Format { get; }

        public DepthStencilMode DepthStencilMode { get; }

        public Target Target { get; }

        public SwizzleComponent SwizzleR { get; }
        public SwizzleComponent SwizzleG { get; }
        public SwizzleComponent SwizzleB { get; }
        public SwizzleComponent SwizzleA { get; }

        public TextureCreateInfo(
            int              width,
            int              height,
            int              depth,
            int              levels,
            int              samples,
            int              blockWidth,
            int              blockHeight,
            int              bytesPerPixel,
            Format           format,
            DepthStencilMode depthStencilMode,
            Target           target,
            SwizzleComponent swizzleR,
            SwizzleComponent swizzleG,
            SwizzleComponent swizzleB,
            SwizzleComponent swizzleA)
        {
            Width            = width;
            Height           = height;
            Depth            = depth;
            Levels           = levels;
            Samples          = samples;
            BlockWidth       = blockWidth;
            BlockHeight      = blockHeight;
            BytesPerPixel    = bytesPerPixel;
            Format           = format;
            DepthStencilMode = depthStencilMode;
            Target           = target;
            SwizzleR         = swizzleR;
            SwizzleG         = swizzleG;
            SwizzleB         = swizzleB;
            SwizzleA         = swizzleA;
        }

        public int GetMipSize(int level)
        {
            return GetMipStride(level) * GetLevelHeight(level) * GetLevelDepth(level);
        }

        public int GetMipSize2D(int level)
        {
            return GetMipStride(level) * GetLevelHeight(level);
        }

        public int GetMipStride(int level)
        {
            return BitUtils.AlignUp(GetLevelWidth(level) * BytesPerPixel, 4);
        }

        private int GetLevelWidth(int level)
        {
            return BitUtils.DivRoundUp(GetLevelSize(Width, level), BlockWidth);
        }

        private int GetLevelHeight(int level)
        {
            return BitUtils.DivRoundUp(GetLevelSize(Height, level), BlockHeight);
        }

        private int GetLevelDepth(int level)
        {
            return Target == Target.Texture3D ? GetLevelSize(Depth, level) : GetLayers();
        }

        public int GetDepthOrLayers()
        {
            return Target == Target.Texture3D ? Depth : GetLayers();
        }

        public int GetLayers()
        {
            if (Target == Target.Texture2DArray ||
                Target == Target.Texture2DMultisampleArray ||
                Target == Target.CubemapArray)
            {
                return Depth;
            }
            else if (Target == Target.Cubemap)
            {
                return 6;
            }

            return 1;
        }

        public int GetLevelsClamped()
        {
            int maxSize = Width;

            if (Target != Target.Texture1D &&
                Target != Target.Texture1DArray)
            {
                maxSize = Math.Max(maxSize, Height);
            }

            if (Target == Target.Texture3D)
            {
                maxSize = Math.Max(maxSize, Depth);
            }

            int maxLevels = BitOperations.Log2((uint)maxSize) + 1;
            return Math.Min(Levels, maxLevels);
        }

        private static int GetLevelSize(int size, int level)
        {
            return Math.Max(1, size >> level);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        bool IEquatable<TextureCreateInfo>.Equals(TextureCreateInfo other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   Depth == other.Depth &&
                   Levels == other.Levels &&
                   Samples == other.Samples &&
                   BlockWidth == other.BlockWidth &&
                   BlockHeight == other.BlockHeight &&
                   BytesPerPixel == other.BytesPerPixel &&
                   Format == other.Format &&
                   DepthStencilMode == other.DepthStencilMode &&
                   Target == other.Target &&
                   SwizzleR == other.SwizzleR &&
                   SwizzleG == other.SwizzleG &&
                   SwizzleB == other.SwizzleB &&
                   SwizzleA == other.SwizzleA;
        }
    }
}
