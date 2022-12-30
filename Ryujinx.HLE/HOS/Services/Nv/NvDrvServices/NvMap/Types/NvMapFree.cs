using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap
{
    [StructLayout(LayoutKind.Sequential)]
    struct NvMapFree
    {
        public int   Handle;
        public int   Padding;
        public ulong Address;
        public int   Size;
        public int   Flags;
    }
}