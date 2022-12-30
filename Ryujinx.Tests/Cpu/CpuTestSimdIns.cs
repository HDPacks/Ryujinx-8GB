#define SimdIns

using ARMeilleure.State;

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("SimdIns")]
    public sealed class CpuTestSimdIns : CpuTest
    {
#if SimdIns

#region "ValueSource"
        private static ulong[] _1D_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                 0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul };
        }

        private static ulong[] _2S_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7FFFFFFF7FFFFFFFul,
                                 0x8000000080000000ul, 0xFFFFFFFFFFFFFFFFul };
        }

        private static ulong[] _4H_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7FFF7FFF7FFF7FFFul,
                                 0x8000800080008000ul, 0xFFFFFFFFFFFFFFFFul };
        }

        private static ulong[] _8B_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7F7F7F7F7F7F7F7Ful,
                                 0x8080808080808080ul, 0xFFFFFFFFFFFFFFFFul };
        }

        private static ulong[] _8B4H_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7F7F7F7F7F7F7F7Ful,
                                 0x8080808080808080ul, 0x7FFF7FFF7FFF7FFFul,
                                 0x8000800080008000ul, 0xFFFFFFFFFFFFFFFFul };
        }

        private static ulong[] _8B4H2S_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7F7F7F7F7F7F7F7Ful,
                                 0x8080808080808080ul, 0x7FFF7FFF7FFF7FFFul,
                                 0x8000800080008000ul, 0x7FFFFFFF7FFFFFFFul,
                                 0x8000000080000000ul, 0xFFFFFFFFFFFFFFFFul };
        }

        private static uint[] _W_()
        {
            return new uint[] { 0x00000000u, 0x0000007Fu,
                                0x00000080u, 0x000000FFu,
                                0x00007FFFu, 0x00008000u,
                                0x0000FFFFu, 0x7FFFFFFFu,
                                0x80000000u, 0xFFFFFFFFu };
        }

        private static ulong[] _X_()
        {
            return new ulong[] { 0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                 0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul };
        }
#endregion

        private const int RndCnt      = 2;
        private const int RndCntIndex = 2;

        [Test, Pairwise, Description("DUP <Vd>.<T>, W<n>")]
        public void Dup_Gp_W([Values(0u)]      uint rd,
                             [Values(1u, 31u)] uint rn,
                             [ValueSource("_W_")] [Random(RndCnt)] uint wn,
                             [Values(0, 1, 2)] int size,  // Q0: <8B,  4H, 2S>
                             [Values(0b0u, 0b1u)] uint q) // Q1: <16B, 8H, 4S>
        {
            uint imm5 = (1u << size) & 0x1Fu;

            uint opcode = 0x0E000C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= ((q & 1) << 30);

            uint  w31 = TestContext.CurrentContext.Random.NextUInt();
            ulong z   = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, x1: wn, x31: w31, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP <Vd>.<T>, X<n>")]
        public void Dup_Gp_X([Values(0u)]      uint rd,
                             [Values(1u, 31u)] uint rn,
                             [ValueSource("_X_")] [Random(RndCnt)] ulong xn)
        {
            uint opcode = 0x4E080C00; // DUP V0.2D, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();
            ulong z   = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, x1: xn, x31: x31, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP B0, V1.B[<index>]")]
        public void Dup_S_B([ValueSource("_8B_")] [Random(RndCnt)] ulong a,
                            [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint index)
        {
            const int size = 0;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x5E000420; // RESERVED
            opcode |= (imm5 << 16);

            ulong z = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP H0, V1.H[<index>]")]
        public void Dup_S_H([ValueSource("_4H_")] [Random(RndCnt)] ulong a,
                            [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint index)
        {
            const int size = 1;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x5E000420; // RESERVED
            opcode |= (imm5 << 16);

            ulong z = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP S0, V1.S[<index>]")]
        public void Dup_S_S([ValueSource("_2S_")] [Random(RndCnt)] ulong a,
                            [Values(0u, 1u, 2u, 3u)] uint index)
        {
            const int size = 2;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x5E000420; // RESERVED
            opcode |= (imm5 << 16);

            ulong z = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP D0, V1.D[<index>]")]
        public void Dup_S_D([ValueSource("_1D_")] [Random(RndCnt)] ulong a,
                            [Values(0u, 1u)] uint index)
        {
            const int size = 3;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x5E000420; // RESERVED
            opcode |= (imm5 << 16);

            ulong z = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP <Vd>.<T>, <Vn>.B[<index>]")]
        public void Dup_V_8B_16B([Values(0u)]     uint rd,
                                 [Values(1u, 0u)] uint rn,
                                 [ValueSource("_8B_")] [Random(RndCnt)] ulong z,
                                 [ValueSource("_8B_")] [Random(RndCnt)] ulong a,
                                 [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint index,
                                 [Values(0b0u, 0b1u)] uint q) // <8B, 16B>
        {
            const int size = 0;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP <Vd>.<T>, <Vn>.H[<index>]")]
        public void Dup_V_4H_8H([Values(0u)]     uint rd,
                                [Values(1u, 0u)] uint rn,
                                [ValueSource("_4H_")] [Random(RndCnt)] ulong z,
                                [ValueSource("_4H_")] [Random(RndCnt)] ulong a,
                                [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint index,
                                [Values(0b0u, 0b1u)] uint q) // <4H, 8H>
        {
            const int size = 1;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP <Vd>.<T>, <Vn>.S[<index>]")]
        public void Dup_V_2S_4S([Values(0u)]     uint rd,
                                [Values(1u, 0u)] uint rn,
                                [ValueSource("_2S_")] [Random(RndCnt)] ulong z,
                                [ValueSource("_2S_")] [Random(RndCnt)] ulong a,
                                [Values(0u, 1u, 2u, 3u)] uint index,
                                [Values(0b0u, 0b1u)] uint q) // <2S, 4S>
        {
            const int size = 2;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("DUP <Vd>.<T>, <Vn>.D[<index>]")]
        public void Dup_V_2D([Values(0u)]     uint rd,
                             [Values(1u, 0u)] uint rn,
                             [ValueSource("_1D_")] [Random(RndCnt)] ulong z,
                             [ValueSource("_1D_")] [Random(RndCnt)] ulong a,
                             [Values(0u, 1u)] uint index,
                             [Values(0b1u)] uint q) // <2D>
        {
            const int size = 3;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.B[<index>], W<n>")]
        public void Ins_Gp_WB([Values(0u)]      uint rd,
                              [Values(1u, 31u)] uint rn,
                              [ValueSource("_8B_")] [Random(RndCnt)] ulong z,
                              [ValueSource("_W_")] [Random(RndCnt)] uint wn,
                              [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint index)
        {
            const int size = 0;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E001C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, x1: wn, x31: w31, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.H[<index>], W<n>")]
        public void Ins_Gp_WH([Values(0u)]      uint rd,
                              [Values(1u, 31u)] uint rn,
                              [ValueSource("_4H_")] [Random(RndCnt)] ulong z,
                              [ValueSource("_W_")] [Random(RndCnt)] uint wn,
                              [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint index)
        {
            const int size = 1;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E001C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, x1: wn, x31: w31, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.S[<index>], W<n>")]
        public void Ins_Gp_WS([Values(0u)]      uint rd,
                              [Values(1u, 31u)] uint rn,
                              [ValueSource("_2S_")] [Random(RndCnt)] ulong z,
                              [ValueSource("_W_")] [Random(RndCnt)] uint wn,
                              [Values(0u, 1u, 2u, 3u)] uint index)
        {
            const int size = 2;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E001C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, x1: wn, x31: w31, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.D[<index>], X<n>")]
        public void Ins_Gp_XD([Values(0u)]      uint rd,
                              [Values(1u, 31u)] uint rn,
                              [ValueSource("_1D_")] [Random(RndCnt)] ulong z,
                              [ValueSource("_X_")] [Random(RndCnt)] ulong xn,
                              [Values(0u, 1u)] uint index)
        {
            const int size = 3;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E001C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();
            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, x1: xn, x31: x31, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.B[<index1>], <Vn>.B[<index2>]")]
        public void Ins_V_BB([Values(0u)]     uint rd,
                             [Values(1u, 0u)] uint rn,
                             [ValueSource("_8B_")] [Random(RndCnt)] ulong z,
                             [ValueSource("_8B_")] [Random(RndCnt)] ulong a,
                             [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint dstIndex,
                             [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint srcIndex)
        {
            const int size = 0;

            uint imm5 = (dstIndex << (size + 1) | 1u << size) & 0x1Fu;
            uint imm4 = (srcIndex << size) & 0xFu;

            uint opcode = 0x6E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= (imm4 << 11);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.H[<index1>], <Vn>.H[<index2>]")]
        public void Ins_V_HH([Values(0u)]     uint rd,
                             [Values(1u, 0u)] uint rn,
                             [ValueSource("_4H_")] [Random(RndCnt)] ulong z,
                             [ValueSource("_4H_")] [Random(RndCnt)] ulong a,
                             [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint dstIndex,
                             [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint srcIndex)
        {
            const int size = 1;

            uint imm5 = (dstIndex << (size + 1) | 1u << size) & 0x1Fu;
            uint imm4 = (srcIndex << size) & 0xFu;

            uint opcode = 0x6E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= (imm4 << 11);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.S[<index1>], <Vn>.S[<index2>]")]
        public void Ins_V_SS([Values(0u)]     uint rd,
                             [Values(1u, 0u)] uint rn,
                             [ValueSource("_2S_")] [Random(RndCnt)] ulong z,
                             [ValueSource("_2S_")] [Random(RndCnt)] ulong a,
                             [Values(0u, 1u, 2u, 3u)] uint dstIndex,
                             [Values(0u, 1u, 2u, 3u)] uint srcIndex)
        {
            const int size = 2;

            uint imm5 = (dstIndex << (size + 1) | 1u << size) & 0x1Fu;
            uint imm4 = (srcIndex << size) & 0xFu;

            uint opcode = 0x6E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= (imm4 << 11);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("INS <Vd>.D[<index1>], <Vn>.D[<index2>]")]
        public void Ins_V_DD([Values(0u)]     uint rd,
                             [Values(1u, 0u)] uint rn,
                             [ValueSource("_1D_")] [Random(RndCnt)] ulong z,
                             [ValueSource("_1D_")] [Random(RndCnt)] ulong a,
                             [Values(0u, 1u)] uint dstIndex,
                             [Values(0u, 1u)] uint srcIndex)
        {
            const int size = 3;

            uint imm5 = (dstIndex << (size + 1) | 1u << size) & 0x1Fu;
            uint imm4 = (srcIndex << size) & 0xFu;

            uint opcode = 0x6E000400; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);
            opcode |= (imm4 << 11);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, v0: v0, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMOV <Wd>, <Vn>.B[<index>]")]
        public void Smov_S_BW([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_8B_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint index)
        {
            const int size = 0;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E002C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x0 = (ulong)TestContext.CurrentContext.Random.NextUInt() << 32;
            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x0: x0, x31: w31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMOV <Wd>, <Vn>.H[<index>]")]
        public void Smov_S_HW([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_4H_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint index)
        {
            const int size = 1;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E002C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x0 = (ulong)TestContext.CurrentContext.Random.NextUInt() << 32;
            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x0: x0, x31: w31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMOV <Xd>, <Vn>.B[<index>]")]
        public void Smov_S_BX([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_8B_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint index)
        {
            const int size = 0;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E002C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x31: x31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMOV <Xd>, <Vn>.H[<index>]")]
        public void Smov_S_HX([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_4H_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint index)
        {
            const int size = 1;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E002C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x31: x31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SMOV <Xd>, <Vn>.S[<index>]")]
        public void Smov_S_SX([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_2S_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 1u, 2u, 3u)] uint index)
        {
            const int size = 2;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E002C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x31: x31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UMOV <Wd>, <Vn>.B[<index>]")]
        public void Umov_S_BW([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_8B_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 15u)] [Random(1u, 14u, RndCntIndex)] uint index)
        {
            const int size = 0;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E003C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x0 = (ulong)TestContext.CurrentContext.Random.NextUInt() << 32;
            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x0: x0, x31: w31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UMOV <Wd>, <Vn>.H[<index>]")]
        public void Umov_S_HW([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_4H_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 7u)] [Random(1u, 6u, RndCntIndex)] uint index)
        {
            const int size = 1;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E003C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x0 = (ulong)TestContext.CurrentContext.Random.NextUInt() << 32;
            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x0: x0, x31: w31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UMOV <Wd>, <Vn>.S[<index>]")]
        public void Umov_S_SW([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_2S_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 1u, 2u, 3u)] uint index)
        {
            const int size = 2;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x0E003C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x0 = (ulong)TestContext.CurrentContext.Random.NextUInt() << 32;
            uint w31 = TestContext.CurrentContext.Random.NextUInt();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x0: x0, x31: w31, v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UMOV <Xd>, <Vn>.D[<index>]")]
        public void Umov_S_DX([Values(0u, 31u)] uint rd,
                              [Values(1u)]      uint rn,
                              [ValueSource("_1D_")] [Random(RndCnt)] ulong a,
                              [Values(0u, 1u)] uint index)
        {
            const int size = 3;

            uint imm5 = (index << (size + 1) | 1u << size) & 0x1Fu;

            uint opcode = 0x4E003C00; // RESERVED
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= (imm5 << 16);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();
            V128 v1 = MakeVectorE0E1(a, a);

            SingleOpcode(opcode, x31: x31, v1: v1);

            CompareAgainstUnicorn();
        }
#endif
    }
}
