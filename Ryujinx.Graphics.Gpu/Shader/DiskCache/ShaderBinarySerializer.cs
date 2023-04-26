using Ryujinx.Common;
using Ryujinx.Common.Memory;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.Graphics.Gpu.Shader.DiskCache
{
    static class ShaderBinarySerializer
    {
        public static byte[] Pack(ShaderSource[] sources)
        {
            using MemoryStream output = MemoryStreamManager.Shared.GetStream();

            output.Write(sources.Length);

            foreach (ShaderSource source in sources)
            {
                output.Write((int)source.Stage);
                output.Write(source.BinaryCode.Length);
                output.Write(source.BinaryCode);
            }

            return output.ToArray();
        }

        public static ShaderSource[] Unpack(CachedShaderStage[] stages, byte[] code)
        {
            using MemoryStream input = new MemoryStream(code);
            using BinaryReader reader = new BinaryReader(input);

            List<ShaderSource> output = new List<ShaderSource>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                ShaderStage stage = (ShaderStage)reader.ReadInt32();
                int binaryCodeLength = reader.ReadInt32();
                byte[] binaryCode = reader.ReadBytes(binaryCodeLength);

                output.Add(new ShaderSource(binaryCode, GetBindings(stages, stage), stage, TargetLanguage.Spirv));
            }

            return output.ToArray();
        }

        private static ShaderBindings GetBindings(CachedShaderStage[] stages, ShaderStage stage)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                CachedShaderStage currentStage = stages[i];

                if (currentStage?.Info != null && currentStage.Info.Stage == stage)
                {
                    return ShaderCache.GetBindings(currentStage.Info);
                }
            }

            return new ShaderBindings(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
        }
    }
}