using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;
using static Ryujinx.Graphics.Shader.Translation.GlobalMemory;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class Rewriter
    {
        public static void RunPass(BasicBlock[] blocks, ShaderConfig config)
        {
            bool isVertexShader = config.Stage == ShaderStage.Vertex;
            bool hasConstantBufferDrawParameters = config.GpuAccessor.QueryHasConstantBufferDrawParameters();
            bool supportsSnormBufferTextureFormat = config.GpuAccessor.QueryHostSupportsSnormBufferTextureFormat();

            for (int blkIndex = 0; blkIndex < blocks.Length; blkIndex++)
            {
                BasicBlock block = blocks[blkIndex];

                for (LinkedListNode<INode> node = block.Operations.First; node != null;)
                {
                    if (node.Value is not Operation operation)
                    {
                        node = node.Next;
                        continue;
                    }

                    if (isVertexShader)
                    {
                        if (hasConstantBufferDrawParameters)
                        {
                            if (ReplaceConstantBufferWithDrawParameters(operation))
                            {
                                config.SetUsedFeature(FeatureFlags.DrawParameters);
                            }
                        }
                        else if (HasConstantBufferDrawParameters(operation))
                        {
                            config.SetUsedFeature(FeatureFlags.DrawParameters);
                        }
                    }

                    LinkedListNode<INode> nextNode = node.Next;

                    if (operation is TextureOperation texOp)
                    {
                        if (texOp.Inst == Instruction.TextureSample)
                        {
                            node = RewriteTextureSample(node, config);

                            if (texOp.Type == SamplerType.TextureBuffer && !supportsSnormBufferTextureFormat)
                            {
                                node = InsertSnormNormalization(node, config);
                            }
                        }

                        nextNode = node.Next;
                    }
                    else if (UsesGlobalMemory(operation.Inst))
                    {
                        nextNode = RewriteGlobalAccess(node, config)?.Next ?? nextNode;
                    }

                    node = nextNode;
                }
            }
        }

        private static LinkedListNode<INode> RewriteGlobalAccess(LinkedListNode<INode> node, ShaderConfig config)
        {
            Operation operation = (Operation)node.Value;

            bool isAtomic = operation.Inst.IsAtomic();
            bool isStg16Or8 = operation.Inst == Instruction.StoreGlobal16 || operation.Inst == Instruction.StoreGlobal8;
            bool isWrite = isAtomic || operation.Inst == Instruction.StoreGlobal || isStg16Or8;

            Operation storageOp = null;

            Operand PrependOperation(Instruction inst, params Operand[] sources)
            {
                Operand local = Local();

                node.List.AddBefore(node, new Operation(inst, local, sources));

                return local;
            }

            Operand PrependExistingOperation(Operation operation)
            {
                Operand local = Local();

                operation.Dest = local;
                node.List.AddBefore(node, operation);

                return local;
            }

            Operand addrLow  = operation.GetSource(0);
            Operand addrHigh = operation.GetSource(1);

            Operand sbBaseAddrLow = Const(0);
            Operand sbSlot        = Const(0);

            Operand alignMask = Const(-config.GpuAccessor.QueryHostStorageBufferOffsetAlignment());

            Operand BindingRangeCheck(int cbOffset, out Operand baseAddrLow)
            {
                baseAddrLow = Cbuf(0, cbOffset);
                Operand baseAddrHigh = Cbuf(0, cbOffset + 1);
                Operand size = Cbuf(0, cbOffset + 2);

                Operand offset = PrependOperation(Instruction.Subtract, addrLow, baseAddrLow);
                Operand borrow = PrependOperation(Instruction.CompareLessU32, addrLow, baseAddrLow);

                Operand inRangeLow = PrependOperation(Instruction.CompareLessU32, offset, size);

                Operand addrHighBorrowed = PrependOperation(Instruction.Add, addrHigh, borrow);

                Operand inRangeHigh = PrependOperation(Instruction.CompareEqual, addrHighBorrowed, baseAddrHigh);

                return PrependOperation(Instruction.BitwiseAnd, inRangeLow, inRangeHigh);
            }

            int sbUseMask = config.AccessibleStorageBuffersMask;

            while (sbUseMask != 0)
            {
                int slot = BitOperations.TrailingZeroCount(sbUseMask);

                sbUseMask &= ~(1 << slot);

                config.SetUsedStorageBuffer(slot, isWrite);

                int cbOffset = GetStorageCbOffset(config.Stage, slot);

                Operand inRange = BindingRangeCheck(cbOffset, out Operand baseAddrLow);

                sbBaseAddrLow = PrependOperation(Instruction.ConditionalSelect, inRange, baseAddrLow, sbBaseAddrLow);
                sbSlot        = PrependOperation(Instruction.ConditionalSelect, inRange, Const(slot), sbSlot);
            }

            if (config.AccessibleStorageBuffersMask != 0)
            {
                Operand baseAddrTrunc = PrependOperation(Instruction.BitwiseAnd, sbBaseAddrLow, alignMask);
                Operand byteOffset    = PrependOperation(Instruction.Subtract, addrLow, baseAddrTrunc);

                Operand[] sources = new Operand[operation.SourcesCount];

                sources[0] = sbSlot;

                if (isStg16Or8)
                {
                    sources[1] = byteOffset;
                }
                else
                {
                    sources[1] = PrependOperation(Instruction.ShiftRightU32, byteOffset, Const(2));
                }

                for (int index = 2; index < operation.SourcesCount; index++)
                {
                    sources[index] = operation.GetSource(index);
                }

                if (isAtomic)
                {
                    Instruction inst = (operation.Inst & ~Instruction.MrMask) | Instruction.MrStorage;

                    storageOp = new Operation(inst, operation.Dest, sources);
                }
                else if (operation.Inst == Instruction.LoadGlobal)
                {
                    storageOp = new Operation(Instruction.LoadStorage, operation.Dest, sources);
                }
                else
                {
                    Instruction storeInst = operation.Inst switch
                    {
                        Instruction.StoreGlobal16 => Instruction.StoreStorage16,
                        Instruction.StoreGlobal8 => Instruction.StoreStorage8,
                        _ => Instruction.StoreStorage
                    };

                    storageOp = new Operation(storeInst, null, sources);
                }
            }
            else if (operation.Dest != null)
            {
                storageOp = new Operation(Instruction.Copy, operation.Dest, Const(0));
            }

            if (operation.Inst == Instruction.LoadGlobal)
            {
                int cbeUseMask = config.AccessibleConstantBuffersMask;

                while (cbeUseMask != 0)
                {
                    int slot = BitOperations.TrailingZeroCount(cbeUseMask);
                    int cbSlot = UbeFirstCbuf + slot;

                    cbeUseMask &= ~(1 << slot);

                    config.SetUsedConstantBuffer(cbSlot);

                    Operand previousResult = PrependExistingOperation(storageOp);

                    int cbOffset = GetConstantUbeOffset(slot);

                    Operand inRange = BindingRangeCheck(cbOffset, out Operand baseAddrLow);

                    Operand baseAddrTruncConst = PrependOperation(Instruction.BitwiseAnd, baseAddrLow, alignMask);
                    Operand byteOffsetConst = PrependOperation(Instruction.Subtract, addrLow, baseAddrTruncConst);

                    Operand cbIndex = PrependOperation(Instruction.ShiftRightU32, byteOffsetConst, Const(2));

                    Operand[] sourcesCb = new Operand[operation.SourcesCount];

                    sourcesCb[0] = Const(cbSlot);
                    sourcesCb[1] = cbIndex;

                    for (int index = 2; index < operation.SourcesCount; index++)
                    {
                        sourcesCb[index] = operation.GetSource(index);
                    }

                    Operand ldcResult = PrependOperation(Instruction.LoadConstant, sourcesCb);

                    storageOp = new Operation(Instruction.ConditionalSelect, operation.Dest, inRange, ldcResult, previousResult);
                }
            }

            for (int index = 0; index < operation.SourcesCount; index++)
            {
                operation.SetSource(index, null);
            }

            LinkedListNode<INode> oldNode = node;
            LinkedList<INode> oldNodeList = oldNode.List;

            if (storageOp != null)
            {
                node = node.List.AddBefore(node, storageOp);
            }
            else
            {
                node = null;
            }

            oldNodeList.Remove(oldNode);

            return node;
        }

        private static LinkedListNode<INode> RewriteTextureSample(LinkedListNode<INode> node, ShaderConfig config)
        {
            TextureOperation texOp = (TextureOperation)node.Value;

            bool hasOffset  = (texOp.Flags & TextureFlags.Offset)  != 0;
            bool hasOffsets = (texOp.Flags & TextureFlags.Offsets) != 0;

            bool hasInvalidOffset = (hasOffset || hasOffsets) && !config.GpuAccessor.QueryHostSupportsNonConstantTextureOffset();

            bool isBindless = (texOp.Flags & TextureFlags.Bindless) != 0;

            bool isCoordNormalized = isBindless || config.GpuAccessor.QueryTextureCoordNormalized(texOp.Handle, texOp.CbufSlot);

            if (!hasInvalidOffset && isCoordNormalized)
            {
                return node;
            }

            bool isGather       = (texOp.Flags & TextureFlags.Gather)      != 0;
            bool hasDerivatives = (texOp.Flags & TextureFlags.Derivatives) != 0;
            bool intCoords      = (texOp.Flags & TextureFlags.IntCoords)   != 0;
            bool hasLodBias     = (texOp.Flags & TextureFlags.LodBias)     != 0;
            bool hasLodLevel    = (texOp.Flags & TextureFlags.LodLevel)    != 0;

            bool isArray       = (texOp.Type & SamplerType.Array)       != 0;
            bool isIndexed     = (texOp.Type & SamplerType.Indexed)     != 0;
            bool isMultisample = (texOp.Type & SamplerType.Multisample) != 0;
            bool isShadow      = (texOp.Type & SamplerType.Shadow)      != 0;

            int coordsCount = texOp.Type.GetDimensions();

            int offsetsCount;

            if (hasOffsets)
            {
                offsetsCount = coordsCount * 4;
            }
            else if (hasOffset)
            {
                offsetsCount = coordsCount;
            }
            else
            {
                offsetsCount = 0;
            }

            Operand[] offsets = new Operand[offsetsCount];
            Operand[] sources = new Operand[texOp.SourcesCount - offsetsCount];

            int copyCount = 0;

            if (isBindless || isIndexed)
            {
                copyCount++;
            }

            Operand[] lodSources = new Operand[copyCount + coordsCount];

            for (int index = 0; index < lodSources.Length; index++)
            {
                lodSources[index] = texOp.GetSource(index);
            }

            copyCount += coordsCount;

            if (isArray)
            {
                copyCount++;
            }

            if (isShadow)
            {
                copyCount++;
            }

            if (hasDerivatives)
            {
                copyCount += coordsCount * 2;
            }

            if (isMultisample)
            {
                copyCount++;
            }
            else if (hasLodLevel)
            {
                copyCount++;
            }

            int srcIndex = 0;
            int dstIndex = 0;

            for (int index = 0; index < copyCount; index++)
            {
                sources[dstIndex++] = texOp.GetSource(srcIndex++);
            }

            bool areAllOffsetsConstant = true;

            for (int index = 0; index < offsetsCount; index++)
            {
                Operand offset = texOp.GetSource(srcIndex++);

                areAllOffsetsConstant &= offset.Type == OperandType.Constant;

                offsets[index] = offset;
            }

            hasInvalidOffset &= !areAllOffsetsConstant;

            if (!hasInvalidOffset && isCoordNormalized)
            {
                return node;
            }

            if (hasLodBias)
            {
               sources[dstIndex++] = texOp.GetSource(srcIndex++);
            }

            if (isGather && !isShadow)
            {
               sources[dstIndex++] = texOp.GetSource(srcIndex++);
            }

            int coordsIndex = isBindless || isIndexed ? 1 : 0;

            int componentIndex = texOp.Index;

            Operand Float(Operand value)
            {
                Operand res = Local();

                node.List.AddBefore(node, new Operation(Instruction.ConvertS32ToFP32, res, value));

                return res;
            }

            // Emulate non-normalized coordinates by normalizing the coordinates on the shader.
            // Without normalization, the coordinates are expected to the in the [0, W or H] range,
            // and otherwise, it is expected to be in the [0, 1] range.
            // We normalize by dividing the coords by the texture size.
            if (!isCoordNormalized && !intCoords)
            {
                config.SetUsedFeature(FeatureFlags.IntegerSampling);

                int normCoordsCount = (texOp.Type & SamplerType.Mask) == SamplerType.TextureCube ? 2 : coordsCount;

                for (int index = 0; index < normCoordsCount; index++)
                {
                    Operand coordSize = Local();

                    Operand[] texSizeSources;

                    if (isBindless || isIndexed)
                    {
                        texSizeSources = new Operand[] { sources[0], Const(0) };
                    }
                    else
                    {
                        texSizeSources = new Operand[] { Const(0) };
                    }

                    node.List.AddBefore(node, new TextureOperation(
                        Instruction.TextureSize,
                        texOp.Type,
                        texOp.Format,
                        texOp.Flags,
                        texOp.CbufSlot,
                        texOp.Handle,
                        index,
                        new[] { coordSize },
                        texSizeSources));

                    config.SetUsedTexture(Instruction.TextureSize, texOp.Type, texOp.Format, texOp.Flags, texOp.CbufSlot, texOp.Handle);

                    Operand source = sources[coordsIndex + index];

                    Operand coordNormalized = Local();

                    node.List.AddBefore(node, new Operation(Instruction.FP32 | Instruction.Divide, coordNormalized, source, Float(coordSize)));

                    sources[coordsIndex + index] = coordNormalized;
                }
            }

            Operand[] dests = new Operand[texOp.DestsCount];

            for (int i = 0; i < texOp.DestsCount; i++)
            {
                dests[i] = texOp.GetDest(i);
            }

            Operand bindlessHandle = isBindless || isIndexed ? sources[0] : null;

            LinkedListNode<INode> oldNode = node;

            // Technically, non-constant texture offsets are not allowed (according to the spec),
            // however some GPUs does support that.
            // For GPUs where it is not supported, we can replace the instruction with the following:
            // For texture*Offset, we replace it by texture*, and add the offset to the P coords.
            // The offset can be calculated as offset / textureSize(lod), where lod = textureQueryLod(coords).
            // For texelFetchOffset, we replace it by texelFetch and add the offset to the P coords directly.
            // For textureGatherOffset, we split the operation into up to 4 operations, one for each component
            // that is accessed, where each textureGather operation has a different offset for each pixel.
            if (hasInvalidOffset && isGather && !isShadow)
            {
                config.SetUsedFeature(FeatureFlags.IntegerSampling);

                Operand[] newSources = new Operand[sources.Length];

                sources.CopyTo(newSources, 0);

                Operand[] texSizes = InsertTextureSize(node, texOp, lodSources, bindlessHandle, coordsCount);

                int destIndex = 0;

                for (int compIndex = 0; compIndex < 4; compIndex++)
                {
                    if (((texOp.Index >> compIndex) & 1) == 0)
                    {
                        continue;
                    }

                    for (int index = 0; index < coordsCount; index++)
                    {
                        config.SetUsedTexture(Instruction.TextureSize, texOp.Type, texOp.Format, texOp.Flags, texOp.CbufSlot, texOp.Handle);

                        Operand offset = Local();

                        Operand intOffset = offsets[index + (hasOffsets ? compIndex * coordsCount : 0)];

                        node.List.AddBefore(node, new Operation(Instruction.FP32 | Instruction.Divide, offset, Float(intOffset), Float(texSizes[index])));

                        Operand source = sources[coordsIndex + index];

                        Operand coordPlusOffset = Local();

                        node.List.AddBefore(node, new Operation(Instruction.FP32 | Instruction.Add, coordPlusOffset, source, offset));

                        newSources[coordsIndex + index] = coordPlusOffset;
                    }

                    TextureOperation newTexOp = new TextureOperation(
                        Instruction.TextureSample,
                        texOp.Type,
                        texOp.Format,
                        texOp.Flags & ~(TextureFlags.Offset | TextureFlags.Offsets),
                        texOp.CbufSlot,
                        texOp.Handle,
                        1,
                        new[] { dests[destIndex++] },
                        newSources);

                    node = node.List.AddBefore(node, newTexOp);
                }
            }
            else
            {
                if (hasInvalidOffset)
                {
                    if (intCoords)
                    {
                        for (int index = 0; index < coordsCount; index++)
                        {
                            Operand source = sources[coordsIndex + index];

                            Operand coordPlusOffset = Local();

                            node.List.AddBefore(node, new Operation(Instruction.Add, coordPlusOffset, source, offsets[index]));

                            sources[coordsIndex + index] = coordPlusOffset;
                        }
                    }
                    else
                    {
                        config.SetUsedFeature(FeatureFlags.IntegerSampling);

                        Operand[] texSizes = InsertTextureSize(node, texOp, lodSources, bindlessHandle, coordsCount);

                        for (int index = 0; index < coordsCount; index++)
                        {
                            config.SetUsedTexture(Instruction.TextureSize, texOp.Type, texOp.Format, texOp.Flags, texOp.CbufSlot, texOp.Handle);

                            Operand offset = Local();

                            Operand intOffset = offsets[index];

                            node.List.AddBefore(node, new Operation(Instruction.FP32 | Instruction.Divide, offset, Float(intOffset), Float(texSizes[index])));

                            Operand source = sources[coordsIndex + index];

                            Operand coordPlusOffset = Local();

                            node.List.AddBefore(node, new Operation(Instruction.FP32 | Instruction.Add, coordPlusOffset, source, offset));

                            sources[coordsIndex + index] = coordPlusOffset;
                        }
                    }
                }

                TextureOperation newTexOp = new TextureOperation(
                    Instruction.TextureSample,
                    texOp.Type,
                    texOp.Format,
                    texOp.Flags & ~(TextureFlags.Offset | TextureFlags.Offsets),
                    texOp.CbufSlot,
                    texOp.Handle,
                    componentIndex,
                    dests,
                    sources);

                node = node.List.AddBefore(node, newTexOp);
            }

            node.List.Remove(oldNode);

            for (int index = 0; index < texOp.SourcesCount; index++)
            {
                texOp.SetSource(index, null);
            }

            return node;
        }

        private static Operand[] InsertTextureSize(
            LinkedListNode<INode> node,
            TextureOperation texOp,
            Operand[] lodSources,
            Operand bindlessHandle,
            int coordsCount)
        {
            Operand Int(Operand value)
            {
                Operand res = Local();

                node.List.AddBefore(node, new Operation(Instruction.ConvertFP32ToS32, res, value));

                return res;
            }

            Operand[] texSizes = new Operand[coordsCount];

            Operand lod = Local();

            node.List.AddBefore(node, new TextureOperation(
                Instruction.Lod,
                texOp.Type,
                texOp.Format,
                texOp.Flags,
                texOp.CbufSlot,
                texOp.Handle,
                0,
                new[] { lod },
                lodSources));

            for (int index = 0; index < coordsCount; index++)
            {
                texSizes[index] = Local();

                Operand[] texSizeSources;

                if (bindlessHandle != null)
                {
                    texSizeSources = new Operand[] { bindlessHandle, Int(lod) };
                }
                else
                {
                    texSizeSources = new Operand[] { Int(lod) };
                }

                node.List.AddBefore(node, new TextureOperation(
                    Instruction.TextureSize,
                    texOp.Type,
                    texOp.Format,
                    texOp.Flags,
                    texOp.CbufSlot,
                    texOp.Handle,
                    index,
                    new[] { texSizes[index] },
                    texSizeSources));
            }

            return texSizes;
        }

        private static LinkedListNode<INode> InsertSnormNormalization(LinkedListNode<INode> node, ShaderConfig config)
        {
            TextureOperation texOp = (TextureOperation)node.Value;

            // We can't query the format of a bindless texture,
            // because the handle is unknown, it can have any format.
            if (texOp.Flags.HasFlag(TextureFlags.Bindless))
            {
                return node;
            }

            TextureFormat format = config.GpuAccessor.QueryTextureFormat(texOp.Handle, texOp.CbufSlot);

            int maxPositive = format switch
            {
                TextureFormat.R8Snorm           => sbyte.MaxValue,
                TextureFormat.R8G8Snorm         => sbyte.MaxValue,
                TextureFormat.R8G8B8A8Snorm     => sbyte.MaxValue,
                TextureFormat.R16Snorm          => short.MaxValue,
                TextureFormat.R16G16Snorm       => short.MaxValue,
                TextureFormat.R16G16B16A16Snorm => short.MaxValue,
                _                               => 0
            };

            // The value being 0 means that the format is not a SNORM format,
            // so there's nothing to do here.
            if (maxPositive == 0)
            {
                return node;
            }

            // Do normalization. We assume SINT formats are being used
            // as replacement for SNORM (which is not supported).
            for (int i = 0; i < texOp.DestsCount; i++)
            {
                Operand dest = texOp.GetDest(i);

                INode[] uses = dest.UseOps.ToArray();

                Operation convOp = new Operation(Instruction.ConvertS32ToFP32, Local(), dest);
                Operation normOp = new Operation(Instruction.FP32 | Instruction.Multiply, Local(), convOp.Dest, ConstF(1f / maxPositive));

                node = node.List.AddAfter(node, convOp);
                node = node.List.AddAfter(node, normOp);

                foreach (INode useOp in uses)
                {
                    if (useOp is not Operation op)
                    {
                        continue;
                    }

                    // Replace all uses of the texture pixel value with the normalized value.
                    for (int index = 0; index < op.SourcesCount; index++)
                    {
                        if (op.GetSource(index) == dest)
                        {
                            op.SetSource(index, normOp.Dest);
                        }
                    }
                }
            }

            return node;
        }

        private static bool ReplaceConstantBufferWithDrawParameters(Operation operation)
        {
            bool modified = false;

            for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
            {
                Operand src = operation.GetSource(srcIndex);

                if (src.Type == OperandType.ConstantBuffer && src.GetCbufSlot() == 0)
                {
                    switch (src.GetCbufOffset())
                    {
                        case Constants.NvnBaseVertexByteOffset / 4:
                            operation.SetSource(srcIndex, Attribute(AttributeConsts.BaseVertex));
                            modified = true;
                            break;
                        case Constants.NvnBaseInstanceByteOffset / 4:
                            operation.SetSource(srcIndex, Attribute(AttributeConsts.BaseInstance));
                            modified = true;
                            break;
                        case Constants.NvnDrawIndexByteOffset / 4:
                            operation.SetSource(srcIndex, Attribute(AttributeConsts.DrawIndex));
                            modified = true;
                            break;
                    }
                }
            }

            return modified;
        }

        private static bool HasConstantBufferDrawParameters(Operation operation)
        {
            for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
            {
                Operand src = operation.GetSource(srcIndex);

                if (src.Type == OperandType.ConstantBuffer && src.GetCbufSlot() == 0)
                {
                    switch (src.GetCbufOffset())
                    {
                        case Constants.NvnBaseVertexByteOffset / 4:
                        case Constants.NvnBaseInstanceByteOffset / 4:
                        case Constants.NvnDrawIndexByteOffset / 4:
                            return true;
                    }
                }
            }

            return false;
        }
    }
}