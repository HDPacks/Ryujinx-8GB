using Silk.NET.Vulkan;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.Vulkan.MoltenVK
{
    [SupportedOSPlatform("macos")]
    public static partial class MVKInitialization
    {
        [LibraryImport("libMoltenVK.dylib")]
        private static partial Result vkGetMoltenVKConfigurationMVK(IntPtr unusedInstance, out MVKConfiguration config, in IntPtr configSize);

        [LibraryImport("libMoltenVK.dylib")]
        private static partial Result vkSetMoltenVKConfigurationMVK(IntPtr unusedInstance, in MVKConfiguration config, in IntPtr configSize);

        public static void Initialize()
        {
            var configSize = (IntPtr)Marshal.SizeOf<MVKConfiguration>();

            vkGetMoltenVKConfigurationMVK(IntPtr.Zero, out MVKConfiguration config, configSize);

            config.UseMetalArgumentBuffers = true;

            config.SemaphoreSupportStyle = MVKVkSemaphoreSupportStyle.MVK_CONFIG_VK_SEMAPHORE_SUPPORT_STYLE_SINGLE_QUEUE;
            config.SynchronousQueueSubmits = false;

            vkSetMoltenVKConfigurationMVK(IntPtr.Zero, config, configSize);
        }
    }
}