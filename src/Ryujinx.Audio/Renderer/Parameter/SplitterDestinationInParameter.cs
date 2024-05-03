using Ryujinx.Common.Utilities;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Audio.Renderer.Parameter
{
    /// <summary>
    /// Input header for a splitter destination update.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SplitterDestinationInParameter
    {
        /// <summary>
        /// Magic of the input header.
        /// </summary>
        public uint Magic;

        /// <summary>
        /// Target splitter destination data id.
        /// </summary>
        public int Id;

        /// <summary>
        /// Mix buffer volumes storage.
        /// </summary>
        private MixArray _mixBufferVolume;

        /// <summary>
        /// The mix to output the result of the splitter.
        /// </summary>
        public int DestinationId;

        /// <summary>
        /// Set to true if in use.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool IsUsed;

        /// <summary>
        /// Reserved/padding.
        /// </summary>
        private unsafe fixed byte _reserved[3];

        [StructLayout(LayoutKind.Sequential, Size = 4 * Constants.MixBufferCountMax, Pack = 1)]
        private struct MixArray { }

        /// <summary>
        /// Mix buffer volumes.
        /// </summary>
        /// <remarks>Used when a splitter id is specified in the mix.</remarks>
        public Span<float> MixBufferVolume => SpanHelpers.AsSpan<MixArray, float>(ref _mixBufferVolume);

        /// <summary>
        /// The expected constant of any input header.
        /// </summary>
        private const uint ValidMagic = 0x44444E53;

        /// <summary>
        /// Check if the magic is valid.
        /// </summary>
        /// <returns>Returns true if the magic is valid.</returns>
        public readonly bool IsMagicValid()
        {
            return Magic == ValidMagic;
        }
    }
}
