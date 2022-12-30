﻿using System.Runtime.InteropServices;

namespace Ryujinx.Input.Motion.CemuHook.Protocol
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        public uint   MagicString;
        public ushort Version;
        public ushort Length;
        public uint   Crc32;
        public uint   Id;
    }
}