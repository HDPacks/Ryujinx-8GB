﻿using Silk.NET.Vulkan;
using System;
using System.Runtime.Serialization;

namespace Ryujinx.Graphics.Vulkan
{
    static class ResultExtensions
    {
        public static void ThrowOnError(this Result result)
        {
            // Only negative result codes are errors.
            if ((int)result < (int)Result.Success)
            {
                throw new VulkanException(result);
            }
        }
    }

    class VulkanException : Exception
    {
        public VulkanException()
        {
        }

        public VulkanException(Result result) : base($"Unexpected API error \"{result}\".")
        {
        }

        public VulkanException(string message) : base(message)
        {
        }

        public VulkanException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected VulkanException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
