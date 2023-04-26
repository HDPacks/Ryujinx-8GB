﻿using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.Gpu.Engine.Threed
{
    /// <summary>
    /// Draw state.
    /// </summary>
    class DrawState
    {
        /// <summary>
        /// First index to be used for the draw on the index buffer.
        /// </summary>
        public int FirstIndex;

        /// <summary>
        /// Number of indices to be used for the draw on the index buffer.
        /// </summary>
        public int IndexCount;

        /// <summary>
        /// First vertex used on non-indexed draws. This value is stored somewhere else on indexed draws.
        /// </summary>
        public int DrawFirstVertex;

        /// <summary>
        /// Vertex count used on non-indexed draws. Indexed draws have a index count instead.
        /// </summary>
        public int DrawVertexCount;

        /// <summary>
        /// Indicates if the next draw will be a indexed draw.
        /// </summary>
        public bool DrawIndexed;

        /// <summary>
        /// Indicates if the next draw will be a indirect draw.
        /// </summary>
        public bool DrawIndirect;

        /// <summary>
        /// Indicates if any of the currently used vertex shaders reads the instance ID.
        /// </summary>
        public bool VsUsesInstanceId;

        /// <summary>
        /// Indicates if any of the currently used vertex buffers is instanced.
        /// </summary>
        public bool IsAnyVbInstanced;

        /// <summary>
        /// Indicates that the draw is writing the base vertex, base instance and draw index to Constant Buffer 0.
        /// </summary>
        public bool HasConstantBufferDrawParameters;

        /// <summary>
        /// Primitive topology for the next draw.
        /// </summary>
        public PrimitiveTopology Topology;

        /// <summary>
        /// Index buffer data streamer for inline index buffer updates, such as those used in legacy OpenGL.
        /// </summary>
        public IbStreamer IbStreamer = new IbStreamer();
    }
}
