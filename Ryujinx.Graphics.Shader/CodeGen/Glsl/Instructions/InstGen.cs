using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;
using System;

using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenBallot;
using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenCall;
using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenFSI;
using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenHelper;
using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenMemory;
using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenPacking;
using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenVector;
using static Ryujinx.Graphics.Shader.StructuredIr.InstructionInfo;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions
{
    static class InstGen
    {
        public static string GetExpression(CodeGenContext context, IAstNode node)
        {
            if (node is AstOperation operation)
            {
                return GetExpression(context, operation);
            }
            else if (node is AstOperand operand)
            {
                return context.OperandManager.GetExpression(context, operand);
            }

            throw new ArgumentException($"Invalid node type \"{node?.GetType().Name ?? "null"}\".");
        }

        public static string Negate(CodeGenContext context, AstOperation operation, InstInfo info)
        {
            IAstNode src = operation.GetSource(0);

            AggregateType type = GetSrcVarType(operation.Inst, 0);

            string srcExpr = GetSoureExpr(context, src, type);
            string zero;

            if (type == AggregateType.FP64)
            {
                zero = "0.0";
            }
            else
            {
                NumberFormatter.TryFormat(0, type, out zero);
            }

            // Starting in the 496.13 NVIDIA driver, there's an issue with assigning variables to negated expressions.
            // (-expr) does not work, but (0.0 - expr) does. This should be removed once the issue is resolved.

            return $"{zero} - {Enclose(srcExpr, src, operation.Inst, info, false)}";
        }

        private static string GetExpression(CodeGenContext context, AstOperation operation)
        {
            Instruction inst = operation.Inst;

            InstInfo info = GetInstructionInfo(inst);

            if ((info.Type & InstType.Call) != 0)
            {
                bool atomic = (info.Type & InstType.Atomic) != 0;

                int arity = (int)(info.Type & InstType.ArityMask);

                string args = string.Empty;

                for (int argIndex = 0; argIndex < arity; argIndex++)
                {
                    // For shared memory access, the second argument is unused and should be ignored.
                    // It is there to make both storage and shared access have the same number of arguments.
                    // For storage, both inputs are consumed when the argument index is 0, so we should skip it here.
                    if (argIndex == 1 && (atomic || (inst & Instruction.MrMask) == Instruction.MrShared))
                    {
                        continue;
                    }

                    if (argIndex != 0)
                    {
                        args += ", ";
                    }

                    if (argIndex == 0 && atomic)
                    {
                        Instruction memRegion = inst & Instruction.MrMask;

                        switch (memRegion)
                        {
                            case Instruction.MrShared: args += LoadShared(context, operation); break;
                            case Instruction.MrStorage: args += LoadStorage(context, operation); break;

                            default: throw new InvalidOperationException($"Invalid memory region \"{memRegion}\".");
                        }
                    }
                    else
                    {
                        AggregateType dstType = GetSrcVarType(inst, argIndex);

                        args += GetSoureExpr(context, operation.GetSource(argIndex), dstType);
                    }
                }

                return info.OpName + '(' + args + ')';
            }
            else if ((info.Type & InstType.Op) != 0)
            {
                string op = info.OpName;

                // Return may optionally have a return value (and in this case it is unary).
                if (inst == Instruction.Return && operation.SourcesCount != 0)
                {
                    return $"{op} {GetSoureExpr(context, operation.GetSource(0), context.CurrentFunction.ReturnType)}";
                }

                int arity = (int)(info.Type & InstType.ArityMask);

                string[] expr = new string[arity];

                for (int index = 0; index < arity; index++)
                {
                    IAstNode src = operation.GetSource(index);

                    string srcExpr = GetSoureExpr(context, src, GetSrcVarType(inst, index));

                    bool isLhs = arity == 2 && index == 0;

                    expr[index] = Enclose(srcExpr, src, inst, info, isLhs);
                }

                switch (arity)
                {
                    case 0:
                        return op;

                    case 1:
                        return op + expr[0];

                    case 2:
                        return $"{expr[0]} {op} {expr[1]}";

                    case 3:
                        return $"{expr[0]} {op[0]} {expr[1]} {op[1]} {expr[2]}";
                }
            }
            else if ((info.Type & InstType.Special) != 0)
            {
                switch (inst & Instruction.Mask)
                {
                    case Instruction.Ballot:
                        return Ballot(context, operation);

                    case Instruction.Call:
                        return Call(context, operation);

                    case Instruction.FSIBegin:
                        return FSIBegin(context);

                    case Instruction.FSIEnd:
                        return FSIEnd(context);

                    case Instruction.ImageLoad:
                    case Instruction.ImageStore:
                    case Instruction.ImageAtomic:
                        return ImageLoadOrStore(context, operation);

                    case Instruction.LoadAttribute:
                        return LoadAttribute(context, operation);

                    case Instruction.LoadConstant:
                        return LoadConstant(context, operation);

                    case Instruction.LoadLocal:
                        return LoadLocal(context, operation);

                    case Instruction.LoadShared:
                        return LoadShared(context, operation);

                    case Instruction.LoadStorage:
                        return LoadStorage(context, operation);

                    case Instruction.Lod:
                        return Lod(context, operation);

                    case Instruction.Negate:
                        return Negate(context, operation, info);

                    case Instruction.PackDouble2x32:
                        return PackDouble2x32(context, operation);

                    case Instruction.PackHalf2x16:
                        return PackHalf2x16(context, operation);

                    case Instruction.StoreAttribute:
                        return StoreAttribute(context, operation);

                    case Instruction.StoreLocal:
                        return StoreLocal(context, operation);

                    case Instruction.StoreShared:
                        return StoreShared(context, operation);

                    case Instruction.StoreShared16:
                        return StoreShared16(context, operation);

                    case Instruction.StoreShared8:
                        return StoreShared8(context, operation);

                    case Instruction.StoreStorage:
                        return StoreStorage(context, operation);

                    case Instruction.StoreStorage16:
                        return StoreStorage16(context, operation);

                    case Instruction.StoreStorage8:
                        return StoreStorage8(context, operation);

                    case Instruction.TextureSample:
                        return TextureSample(context, operation);

                    case Instruction.TextureSize:
                        return TextureSize(context, operation);

                    case Instruction.UnpackDouble2x32:
                        return UnpackDouble2x32(context, operation);

                    case Instruction.UnpackHalf2x16:
                        return UnpackHalf2x16(context, operation);

                    case Instruction.VectorExtract:
                        return VectorExtract(context, operation);
                }
            }

            throw new InvalidOperationException($"Unexpected instruction type \"{info.Type}\".");
        }
    }
}