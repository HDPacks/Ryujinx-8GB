﻿namespace Ryujinx.Graphics.Vulkan
{
    static class VulkanConfiguration
    {
        public const bool UseFastBufferUpdates = true;
        public const bool UseSlowSafeBlitOnAmd = true;
        public const bool UsePushDescriptors = false;

        public const bool ForceD24S8Unsupported = false;
        public const bool ForceRGB16IntFloatUnsupported = false;
    }
}
