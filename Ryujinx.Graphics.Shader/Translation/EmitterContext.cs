using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation
{
    class EmitterContext
    {
        public DecodedProgram Program { get; }
        public ShaderConfig Config { get; }

        public bool IsNonMain { get; }

        public Block CurrBlock { get; set; }
        public InstOp CurrOp { get; set; }

        public int OperationsCount => _operations.Count;

        private readonly struct BrxTarget
        {
            public readonly Operand Selector;
            public readonly int ExpectedValue;
            public readonly ulong NextTargetAddress;

            public BrxTarget(Operand selector, int expectedValue, ulong nextTargetAddress)
            {
                Selector = selector;
                ExpectedValue = expectedValue;
                NextTargetAddress = nextTargetAddress;
            }
        }

        private class BlockLabel
        {
            public readonly Operand Label;
            public BrxTarget BrxTarget;

            public BlockLabel(Operand label)
            {
                Label = label;
            }
        }

        private readonly List<Operation> _operations;
        private readonly Dictionary<ulong, BlockLabel> _labels;

        public EmitterContext(DecodedProgram program, ShaderConfig config, bool isNonMain)
        {
            Program = program;
            Config = config;
            IsNonMain = isNonMain;
            _operations = new List<Operation>();
            _labels = new Dictionary<ulong, BlockLabel>();

            EmitStart();
        }

        private void EmitStart()
        {
            if (Config.Stage == ShaderStage.Vertex &&
                Config.Options.TargetApi == TargetApi.Vulkan &&
                (Config.Options.Flags & TranslationFlags.VertexA) == 0)
            {
                // Vulkan requires the point size to be always written on the shader if the primitive topology is points.
                this.Copy(Attribute(AttributeConsts.PointSize), ConstF(Config.GpuAccessor.QueryPointSize()));
            }
        }

        public T GetOp<T>() where T : unmanaged
        {
            Debug.Assert(Unsafe.SizeOf<T>() == sizeof(ulong));
            ulong op = CurrOp.RawOpCode;
            return Unsafe.As<ulong, T>(ref op);
        }

        public Operand Add(Instruction inst, Operand dest = null, params Operand[] sources)
        {
            Operation operation = new Operation(inst, dest, sources);

            _operations.Add(operation);

            return dest;
        }

        public (Operand, Operand) Add(Instruction inst, (Operand, Operand) dest, params Operand[] sources)
        {
            Operand[] dests = new[] { dest.Item1, dest.Item2 };

            Operation operation = new Operation(inst, 0, dests, sources);

            Add(operation);

            return dest;
        }

        public void Add(Operation operation)
        {
            _operations.Add(operation);
        }

        public TextureOperation CreateTextureOperation(
            Instruction inst,
            SamplerType type,
            TextureFlags flags,
            int handle,
            int compIndex,
            Operand[] dests,
            params Operand[] sources)
        {
            return CreateTextureOperation(inst, type, TextureFormat.Unknown, flags, handle, compIndex, dests, sources);
        }

        public TextureOperation CreateTextureOperation(
            Instruction inst,
            SamplerType type,
            TextureFormat format,
            TextureFlags flags,
            int handle,
            int compIndex,
            Operand[] dests,
            params Operand[] sources)
        {
            if (!flags.HasFlag(TextureFlags.Bindless))
            {
                Config.SetUsedTexture(inst, type, format, flags, TextureOperation.DefaultCbufSlot, handle);
            }

            return new TextureOperation(inst, type, format, flags, handle, compIndex, dests, sources);
        }

        public void FlagAttributeRead(int attribute)
        {
            if (Config.Stage == ShaderStage.Vertex && attribute == AttributeConsts.InstanceId)
            {
                Config.SetUsedFeature(FeatureFlags.InstanceId);
            }
            else if (Config.Stage == ShaderStage.Fragment)
            {
                switch (attribute)
                {
                    case AttributeConsts.PositionX:
                    case AttributeConsts.PositionY:
                        Config.SetUsedFeature(FeatureFlags.FragCoordXY);
                        break;
                }
            }
        }

        public void FlagAttributeWritten(int attribute)
        {
            if (Config.Stage == ShaderStage.Vertex)
            {
                switch (attribute)
                {
                    case AttributeConsts.ClipDistance0:
                    case AttributeConsts.ClipDistance1:
                    case AttributeConsts.ClipDistance2:
                    case AttributeConsts.ClipDistance3:
                    case AttributeConsts.ClipDistance4:
                    case AttributeConsts.ClipDistance5:
                    case AttributeConsts.ClipDistance6:
                    case AttributeConsts.ClipDistance7:
                        Config.SetClipDistanceWritten((attribute - AttributeConsts.ClipDistance0) / 4);
                        break;
                }
            }

            if (Config.Stage != ShaderStage.Fragment && attribute == AttributeConsts.Layer)
            {
                Config.SetUsedFeature(FeatureFlags.RtLayer);
            }
        }

        public void MarkLabel(Operand label)
        {
            Add(Instruction.MarkLabel, label);
        }

        public Operand GetLabel(ulong address)
        {
            return EnsureBlockLabel(address).Label;
        }

        public void SetBrxTarget(ulong address, Operand selector, int targetValue, ulong nextTargetAddress)
        {
            BlockLabel blockLabel = EnsureBlockLabel(address);
            Debug.Assert(blockLabel.BrxTarget.Selector == null);
            blockLabel.BrxTarget = new BrxTarget(selector, targetValue, nextTargetAddress);
        }

        public void EnterBlock(ulong address)
        {
            BlockLabel blockLabel = EnsureBlockLabel(address);

            MarkLabel(blockLabel.Label);

            BrxTarget brxTarget = blockLabel.BrxTarget;

            if (brxTarget.Selector != null)
            {
                this.BranchIfFalse(GetLabel(brxTarget.NextTargetAddress), this.ICompareEqual(brxTarget.Selector, Const(brxTarget.ExpectedValue)));
            }
        }

        private BlockLabel EnsureBlockLabel(ulong address)
        {
            if (!_labels.TryGetValue(address, out BlockLabel blockLabel))
            {
                blockLabel = new BlockLabel(Label());

                _labels.Add(address, blockLabel);
            }

            return blockLabel;
        }

        public void PrepareForVertexReturn()
        {
            if (Config.GpuAccessor.QueryViewportTransformDisable())
            {
                Operand x = Attribute(AttributeConsts.PositionX | AttributeConsts.LoadOutputMask);
                Operand y = Attribute(AttributeConsts.PositionY | AttributeConsts.LoadOutputMask);
                Operand xScale = Attribute(AttributeConsts.SupportBlockViewInverseX);
                Operand yScale = Attribute(AttributeConsts.SupportBlockViewInverseY);
                Operand negativeOne = ConstF(-1.0f);

                this.Copy(Attribute(AttributeConsts.PositionX), this.FPFusedMultiplyAdd(x, xScale, negativeOne));
                this.Copy(Attribute(AttributeConsts.PositionY), this.FPFusedMultiplyAdd(y, yScale, negativeOne));
            }

            if (Config.Options.TargetApi == TargetApi.Vulkan && Config.GpuAccessor.QueryTransformDepthMinusOneToOne())
            {
                Operand z = Attribute(AttributeConsts.PositionZ | AttributeConsts.LoadOutputMask);
                Operand w = Attribute(AttributeConsts.PositionW | AttributeConsts.LoadOutputMask);
                Operand halfW = this.FPMultiply(w, ConstF(0.5f));

                this.Copy(Attribute(AttributeConsts.PositionZ), this.FPFusedMultiplyAdd(z, ConstF(0.5f), halfW));
            }

            if (Config.Stage != ShaderStage.Geometry && Config.HasLayerInputAttribute)
            {
                Config.SetUsedFeature(FeatureFlags.RtLayer);

                this.Copy(Attribute(AttributeConsts.Layer), Attribute(Config.GpLayerInputAttribute | AttributeConsts.LoadOutputMask));
            }
        }

        public void PrepareForVertexReturn(out Operand oldXLocal, out Operand oldYLocal, out Operand oldZLocal)
        {
            if (Config.GpuAccessor.QueryViewportTransformDisable())
            {
                oldXLocal = Local();
                this.Copy(oldXLocal, Attribute(AttributeConsts.PositionX | AttributeConsts.LoadOutputMask));
                oldYLocal = Local();
                this.Copy(oldYLocal, Attribute(AttributeConsts.PositionY | AttributeConsts.LoadOutputMask));
            }
            else
            {
                oldXLocal = null;
                oldYLocal = null;
            }

            if (Config.Options.TargetApi == TargetApi.Vulkan && Config.GpuAccessor.QueryTransformDepthMinusOneToOne())
            {
                oldZLocal = Local();
                this.Copy(oldZLocal, Attribute(AttributeConsts.PositionZ | AttributeConsts.LoadOutputMask));
            }
            else
            {
                oldZLocal = null;
            }

            PrepareForVertexReturn();
        }

        public void PrepareForReturn()
        {
            if (IsNonMain)
            {
                return;
            }

            if (Config.LastInVertexPipeline &&
                (Config.Stage == ShaderStage.Vertex || Config.Stage == ShaderStage.TessellationEvaluation) &&
                (Config.Options.Flags & TranslationFlags.VertexA) == 0)
            {
                PrepareForVertexReturn();
            }
            else if (Config.Stage == ShaderStage.Geometry)
            {
                void WriteOutput(int index, int primIndex)
                {
                    Operand x = this.LoadAttribute(Const(index), Const(0), Const(primIndex));
                    Operand y = this.LoadAttribute(Const(index + 4), Const(0), Const(primIndex));
                    Operand z = this.LoadAttribute(Const(index + 8), Const(0), Const(primIndex));
                    Operand w = this.LoadAttribute(Const(index + 12), Const(0), Const(primIndex));

                    this.Copy(Attribute(index), x);
                    this.Copy(Attribute(index + 4), y);
                    this.Copy(Attribute(index + 8), z);
                    this.Copy(Attribute(index + 12), w);
                }

                if (Config.GpPassthrough && !Config.GpuAccessor.QueryHostSupportsGeometryShaderPassthrough())
                {
                    int inputVertices = Config.GpuAccessor.QueryPrimitiveTopology().ToInputVertices();

                    for (int primIndex = 0; primIndex < inputVertices; primIndex++)
                    {
                        WriteOutput(AttributeConsts.PositionX, primIndex);

                        int passthroughAttributes = Config.PassthroughAttributes;
                        while (passthroughAttributes != 0)
                        {
                            int index = BitOperations.TrailingZeroCount(passthroughAttributes);
                            WriteOutput(AttributeConsts.UserAttributeBase + index * 16, primIndex);
                            Config.SetOutputUserAttribute(index);
                            passthroughAttributes &= ~(1 << index);
                        }

                        this.EmitVertex();
                    }

                    this.EndPrimitive();
                }
            }
            else if (Config.Stage == ShaderStage.Fragment)
            {
                GenerateAlphaToCoverageDitherDiscard();

                bool supportsBgra = Config.GpuAccessor.QueryHostSupportsBgraFormat();

                if (Config.OmapDepth)
                {
                    Operand dest = Attribute(AttributeConsts.FragmentOutputDepth);

                    Operand src = Register(Config.GetDepthRegister(), RegisterType.Gpr);

                    this.Copy(dest, src);
                }

                AlphaTestOp alphaTestOp = Config.GpuAccessor.QueryAlphaTestCompare();

                if (alphaTestOp != AlphaTestOp.Always && (Config.OmapTargets & 8) != 0)
                {
                    if (alphaTestOp == AlphaTestOp.Never)
                    {
                        this.Discard();
                    }
                    else
                    {
                        Instruction comparator = alphaTestOp switch
                        {
                            AlphaTestOp.Equal => Instruction.CompareEqual,
                            AlphaTestOp.Greater => Instruction.CompareGreater,
                            AlphaTestOp.GreaterOrEqual => Instruction.CompareGreaterOrEqual,
                            AlphaTestOp.Less => Instruction.CompareLess,
                            AlphaTestOp.LessOrEqual => Instruction.CompareLessOrEqual,
                            AlphaTestOp.NotEqual => Instruction.CompareNotEqual,
                            _ => 0
                        };

                        Debug.Assert(comparator != 0, $"Invalid alpha test operation \"{alphaTestOp}\".");

                        Operand alpha = Register(3, RegisterType.Gpr);
                        Operand alphaRef = ConstF(Config.GpuAccessor.QueryAlphaTestReference());
                        Operand alphaPass = Add(Instruction.FP32 | comparator, Local(), alpha, alphaRef);
                        Operand alphaPassLabel = Label();

                        this.BranchIfTrue(alphaPassLabel, alphaPass);
                        this.Discard();
                        this.MarkLabel(alphaPassLabel);
                    }
                }

                int regIndexBase = 0;

                for (int rtIndex = 0; rtIndex < 8; rtIndex++)
                {
                    for (int component = 0; component < 4; component++)
                    {
                        bool componentEnabled = (Config.OmapTargets & (1 << (rtIndex * 4 + component))) != 0;
                        if (!componentEnabled)
                        {
                            continue;
                        }

                        int fragmentOutputColorAttr = AttributeConsts.FragmentOutputColorBase + rtIndex * 16;

                        Operand src = Register(regIndexBase + component, RegisterType.Gpr);

                        // Perform B <-> R swap if needed, for BGRA formats (not supported on OpenGL).
                        if (!supportsBgra && (component == 0 || component == 2))
                        {
                            Operand isBgra = Attribute(AttributeConsts.FragmentOutputIsBgraBase + rtIndex * 4);

                            Operand lblIsBgra = Label();
                            Operand lblEnd = Label();

                            this.BranchIfTrue(lblIsBgra, isBgra);

                            this.Copy(Attribute(fragmentOutputColorAttr + component * 4), src);
                            this.Branch(lblEnd);

                            MarkLabel(lblIsBgra);

                            this.Copy(Attribute(fragmentOutputColorAttr + (2 - component) * 4), src);

                            MarkLabel(lblEnd);
                        }
                        else
                        {
                            this.Copy(Attribute(fragmentOutputColorAttr + component * 4), src);
                        }
                    }

                    bool targetEnabled = (Config.OmapTargets & (0xf << (rtIndex * 4))) != 0;
                    if (targetEnabled)
                    {
                        Config.SetOutputUserAttribute(rtIndex);
                        regIndexBase += 4;
                    }
                }
            }
        }

        private void GenerateAlphaToCoverageDitherDiscard()
        {
            // If the feature is disabled, or alpha is not written, then we're done.
            if (!Config.GpuAccessor.QueryAlphaToCoverageDitherEnable() || (Config.OmapTargets & 8) == 0)
            {
                return;
            }

            // 11 11 11 10 10 10 10 00
            // 11 01 01 01 01 00 00 00
            Operand ditherMask = Const(unchecked((int)0xfbb99110u));

            Operand x = this.BitwiseAnd(this.FP32ConvertToU32(Attribute(AttributeConsts.PositionX)), Const(1));
            Operand y = this.BitwiseAnd(this.FP32ConvertToU32(Attribute(AttributeConsts.PositionY)), Const(1));
            Operand xy = this.BitwiseOr(x, this.ShiftLeft(y, Const(1)));

            Operand alpha = Register(3, RegisterType.Gpr);
            Operand scaledAlpha = this.FPMultiply(this.FPSaturate(alpha), ConstF(8));
            Operand quantizedAlpha = this.IMinimumU32(this.FP32ConvertToU32(scaledAlpha), Const(7));
            Operand shift = this.BitwiseOr(this.ShiftLeft(quantizedAlpha, Const(2)), xy);
            Operand opaque = this.BitwiseAnd(this.ShiftRightU32(ditherMask, shift), Const(1));

            Operand a2cDitherEndLabel = Label();

            this.BranchIfTrue(a2cDitherEndLabel, opaque);
            this.Discard();
            this.MarkLabel(a2cDitherEndLabel);
        }

        public Operation[] GetOperations()
        {
            return _operations.ToArray();
        }
    }
}