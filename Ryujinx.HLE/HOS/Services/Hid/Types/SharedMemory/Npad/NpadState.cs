﻿using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Hid.Types.SharedMemory.Npad
{
    [StructLayout(LayoutKind.Sequential, Size = 0x5000)]
    struct NpadState
    {
        public NpadInternalState InternalState;

        public static NpadState Create()
        {
            return new NpadState
            {
                InternalState = NpadInternalState.Create()
            };
        }
    }
}
