using System;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    /// <summary>
    /// Flags indicating how the render targets should be updated.
    /// </summary>
    [Flags]
    enum RenderTargetUpdateFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// Get render target index from the control register.
        /// </summary>
        UseControl = 1 << 0,

        /// <summary>
        /// Indicates that all render targets are 2D array textures.
        /// </summary>
        Layered = 1 << 1,

        /// <summary>
        /// Indicates that only a single color target will be used.
        /// </summary>
        SingleColor = 1 << 2,

        /// <summary>
        /// Indicates that the depth-stencil target will be used.
        /// </summary>
        UpdateDepthStencil = 1 << 3,

        /// <summary>
        /// Default update flags for draw.
        /// </summary>
        UpdateAll = UseControl | UpdateDepthStencil
    }
}
