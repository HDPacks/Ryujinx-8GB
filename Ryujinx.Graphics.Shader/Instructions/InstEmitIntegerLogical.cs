using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitAluHelper;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        private const int PT = RegisterConsts.PredicateTrueIndex;

        public static void LopR(EmitterContext context)
        {
            InstLopR op = context.GetOp<InstLopR>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcReg(context, op.SrcB);

            EmitLop(context, op.Lop, op.PredicateOp, srcA, srcB, op.Dest, op.DestPred, op.NegA, op.NegB, op.X, op.WriteCC);
        }

        public static void LopI(EmitterContext context)
        {
            InstLopI op = context.GetOp<InstLopI>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitLop(context, op.LogicOp, op.PredicateOp, srcA, srcB, op.Dest, op.DestPred, op.NegA, op.NegB, op.X, op.WriteCC);
        }

        public static void LopC(EmitterContext context)
        {
            InstLopC op = context.GetOp<InstLopC>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitLop(context, op.LogicOp, op.PredicateOp, srcA, srcB, op.Dest, op.DestPred, op.NegA, op.NegB, op.X, op.WriteCC);
        }

        public static void Lop32i(EmitterContext context)
        {
            InstLop32i op = context.GetOp<InstLop32i>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcImm(context, op.Imm32);

            EmitLop(context, op.LogicOp, PredicateOp.F, srcA, srcB, op.Dest, PT, op.NegA, op.NegB, op.X, op.WriteCC);
        }

        public static void Lop3R(EmitterContext context)
        {
            InstLop3R op = context.GetOp<InstLop3R>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcReg(context, op.SrcB);
            var srcC = GetSrcReg(context, op.SrcC);

            EmitLop3(context, op.Imm, op.PredicateOp, srcA, srcB, srcC, op.Dest, op.DestPred, op.X, op.WriteCC);
        }

        public static void Lop3I(EmitterContext context)
        {
            InstLop3I op = context.GetOp<InstLop3I>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));
            var srcC = GetSrcReg(context, op.SrcC);

            EmitLop3(context, op.Imm, PredicateOp.F, srcA, srcB, srcC, op.Dest, PT, false, op.WriteCC);
        }

        public static void Lop3C(EmitterContext context)
        {
            InstLop3C op = context.GetOp<InstLop3C>();

            var srcA = GetSrcReg(context, op.SrcA);
            var srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            var srcC = GetSrcReg(context, op.SrcC);

            EmitLop3(context, op.Imm, PredicateOp.F, srcA, srcB, srcC, op.Dest, PT, false, op.WriteCC);
        }

        private static void EmitLop(
            EmitterContext context,
            LogicOp logicOp,
            PredicateOp predOp,
            Operand srcA,
            Operand srcB,
            int rd,
            int destPred,
            bool invertA,
            bool invertB,
            bool extended,
            bool writeCC)
        {
            srcA = context.BitwiseNot(srcA, invertA);
            srcB = context.BitwiseNot(srcB, invertB);

            Operand res = logicOp switch
            {
                LogicOp.And => res = context.BitwiseAnd(srcA, srcB),
                LogicOp.Or => res = context.BitwiseOr(srcA, srcB),
                LogicOp.Xor => res = context.BitwiseExclusiveOr(srcA, srcB),
                _ => srcB
            };

            EmitLopPredWrite(context, res, predOp, destPred);

            context.Copy(GetDest(rd), res);

            SetZnFlags(context, res, writeCC, extended);
        }

        private static void EmitLop3(
            EmitterContext context,
            int truthTable,
            PredicateOp predOp,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            int rd,
            int destPred,
            bool extended,
            bool writeCC)
        {
            Operand res = Lop3Expression.GetFromTruthTable(context, srcA, srcB, srcC, truthTable);

            EmitLopPredWrite(context, res, predOp, destPred);

            context.Copy(GetDest(rd), res);

            SetZnFlags(context, res, writeCC, extended);
        }

        private static void EmitLopPredWrite(EmitterContext context, Operand result, PredicateOp predOp, int pred)
        {
            if (pred != RegisterConsts.PredicateTrueIndex)
            {
                Operand pRes;

                if (predOp == PredicateOp.F)
                {
                    pRes = Const(IrConsts.False);
                }
                else if (predOp == PredicateOp.T)
                {
                    pRes = Const(IrConsts.True);
                }
                else if (predOp == PredicateOp.Z)
                {
                    pRes = context.ICompareEqual(result, Const(0));
                }
                else /* if (predOp == Pop.Nz) */
                {
                    pRes = context.ICompareNotEqual(result, Const(0));
                }

                context.Copy(Register(pred, RegisterType.Predicate), pRes);
            }
        }
    }
}