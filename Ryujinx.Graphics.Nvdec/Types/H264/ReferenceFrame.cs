﻿using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Types.H264
{
    struct ReferenceFrame
    {
#pragma warning disable CS0649
        public uint Flags;
        public Array2<uint> FieldOrderCnt;
        public uint FrameNum;
#pragma warning restore CS0649

        public uint OutputSurfaceIndex => (uint)Flags & 0x7f;
    }
}
