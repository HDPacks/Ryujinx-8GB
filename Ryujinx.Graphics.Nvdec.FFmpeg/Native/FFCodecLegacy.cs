﻿using System;

namespace Ryujinx.Graphics.Nvdec.FFmpeg.Native
{
    struct FFCodecLegacy<T> where T: struct
    {
#pragma warning disable CS0649
        public T Base;
        public uint CapsInternalOrCbType;
        public int PrivDataSize;
        public IntPtr UpdateThreadContext;
        public IntPtr UpdateThreadContextForUser;
        public IntPtr Defaults;
        public IntPtr InitStaticData;
        public IntPtr Init;
        public IntPtr EncodeSub;
        public IntPtr Encode2;
        public IntPtr Decode;
#pragma warning restore CS0649

        // NOTE: There is more after, but the layout kind of changed a bit and we don't need more than this. This is safe as we only manipulate this behind a reference.
    }
}
