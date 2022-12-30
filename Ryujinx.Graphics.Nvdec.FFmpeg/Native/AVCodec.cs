﻿using System;

namespace Ryujinx.Graphics.Nvdec.FFmpeg.Native
{
    struct AVCodec
    {
#pragma warning disable CS0649
        public unsafe byte* Name;
        public unsafe byte* LongName;
        public int Type;
        public AVCodecID Id;
        public int Capabilities;
        public byte MaxLowRes;
        public unsafe AVRational* SupportedFramerates;
        public IntPtr PixFmts;
        public IntPtr SupportedSamplerates;
        public IntPtr SampleFmts;
        // Deprecated
        public unsafe ulong* ChannelLayouts;
        public unsafe IntPtr PrivClass;
        public IntPtr Profiles;
        public unsafe byte* WrapperName;
        public IntPtr ChLayouts;
#pragma warning restore CS0649
    }
}
