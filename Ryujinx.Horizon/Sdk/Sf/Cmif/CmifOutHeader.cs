﻿using Ryujinx.Horizon.Common;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    struct CmifOutHeader
    {
#pragma warning disable CS0649
        public uint   Magic;
        public uint   Version;
        public Result Result;
        public uint   Token;
#pragma warning restore CS0649
    }
}
