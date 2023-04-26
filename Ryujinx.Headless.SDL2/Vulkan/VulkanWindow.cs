﻿using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Input.HLE;
using Ryujinx.SDL2.Common;
using System;
using System.Runtime.InteropServices;
using static SDL2.SDL;

namespace Ryujinx.Headless.SDL2.Vulkan
{
    class VulkanWindow : WindowBase
    {
        private GraphicsDebugLevel _glLogLevel;

        public VulkanWindow(
            InputManager inputManager,
            GraphicsDebugLevel glLogLevel,
            AspectRatio aspectRatio,
            bool enableMouse,
            HideCursor hideCursor)
            : base(inputManager, glLogLevel, aspectRatio, enableMouse, hideCursor)
        {
            _glLogLevel = glLogLevel;
        }

        public override SDL_WindowFlags GetWindowFlags() => SDL_WindowFlags.SDL_WINDOW_VULKAN;

        protected override void InitializeWindowRenderer() { }

        protected override void InitializeRenderer()
        {
            Renderer?.Window.SetSize(DefaultWidth, DefaultHeight);
            MouseDriver.SetClientSize(DefaultWidth, DefaultHeight);
        }

        private void BasicInvoke(Action action)
        {
            action();
        }

        public unsafe IntPtr CreateWindowSurface(IntPtr instance)
        {
            ulong surfaceHandle = 0;

            Action createSurface = () =>
            {
                if (SDL_Vulkan_CreateSurface(WindowHandle, instance, out surfaceHandle) == SDL_bool.SDL_FALSE)
                {
                    string errorMessage = $"SDL_Vulkan_CreateSurface failed with error \"{SDL_GetError()}\"";

                    Logger.Error?.Print(LogClass.Application, errorMessage);

                    throw new Exception(errorMessage);
                }
            };

            if (SDL2Driver.MainThreadDispatcher != null)
            {
                SDL2Driver.MainThreadDispatcher(createSurface);
            }
            else
            {
                createSurface();
            }

            return (IntPtr)surfaceHandle;
        }

        public unsafe string[] GetRequiredInstanceExtensions()
        {
            if (SDL_Vulkan_GetInstanceExtensions(WindowHandle, out uint extensionsCount, IntPtr.Zero) == SDL_bool.SDL_TRUE)
            {
                IntPtr[] rawExtensions = new IntPtr[(int)extensionsCount];
                string[] extensions = new string[(int)extensionsCount];

                fixed (IntPtr* rawExtensionsPtr = rawExtensions)
                {
                    if (SDL_Vulkan_GetInstanceExtensions(WindowHandle, out extensionsCount, (IntPtr)rawExtensionsPtr) == SDL_bool.SDL_TRUE)
                    {
                        for (int i = 0; i < extensions.Length; i++)
                        {
                            extensions[i] = Marshal.PtrToStringUTF8(rawExtensions[i]);
                        }

                        return extensions;
                    }
                }
            }

            string errorMessage = $"SDL_Vulkan_GetInstanceExtensions failed with error \"{SDL_GetError()}\"";

            Logger.Error?.Print(LogClass.Application, errorMessage);

            throw new Exception(errorMessage);
        }

        protected override void FinalizeWindowRenderer()
        {
            Device.DisposeGpu();
        }

        protected override void SwapBuffers() { }
    }
}