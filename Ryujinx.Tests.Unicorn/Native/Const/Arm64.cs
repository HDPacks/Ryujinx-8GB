// Constants for Unicorn Engine. AUTO-GENERATED FILE, DO NOT EDIT

// ReSharper disable InconsistentNaming
namespace Ryujinx.Tests.Unicorn.Native.Const
{
    public enum Arm64
    {

        // ARM64 CPU

        CPU_ARM64_A57 = 0,
        CPU_ARM64_A53 = 1,
        CPU_ARM64_A72 = 2,
        CPU_ARM64_MAX = 3,
        CPU_ARM64_ENDING = 4,

        // ARM64 registers

        REG_INVALID = 0,
        REG_X29 = 1,
        REG_X30 = 2,
        REG_NZCV = 3,
        REG_SP = 4,
        REG_WSP = 5,
        REG_WZR = 6,
        REG_XZR = 7,
        REG_B0 = 8,
        REG_B1 = 9,
        REG_B2 = 10,
        REG_B3 = 11,
        REG_B4 = 12,
        REG_B5 = 13,
        REG_B6 = 14,
        REG_B7 = 15,
        REG_B8 = 16,
        REG_B9 = 17,
        REG_B10 = 18,
        REG_B11 = 19,
        REG_B12 = 20,
        REG_B13 = 21,
        REG_B14 = 22,
        REG_B15 = 23,
        REG_B16 = 24,
        REG_B17 = 25,
        REG_B18 = 26,
        REG_B19 = 27,
        REG_B20 = 28,
        REG_B21 = 29,
        REG_B22 = 30,
        REG_B23 = 31,
        REG_B24 = 32,
        REG_B25 = 33,
        REG_B26 = 34,
        REG_B27 = 35,
        REG_B28 = 36,
        REG_B29 = 37,
        REG_B30 = 38,
        REG_B31 = 39,
        REG_D0 = 40,
        REG_D1 = 41,
        REG_D2 = 42,
        REG_D3 = 43,
        REG_D4 = 44,
        REG_D5 = 45,
        REG_D6 = 46,
        REG_D7 = 47,
        REG_D8 = 48,
        REG_D9 = 49,
        REG_D10 = 50,
        REG_D11 = 51,
        REG_D12 = 52,
        REG_D13 = 53,
        REG_D14 = 54,
        REG_D15 = 55,
        REG_D16 = 56,
        REG_D17 = 57,
        REG_D18 = 58,
        REG_D19 = 59,
        REG_D20 = 60,
        REG_D21 = 61,
        REG_D22 = 62,
        REG_D23 = 63,
        REG_D24 = 64,
        REG_D25 = 65,
        REG_D26 = 66,
        REG_D27 = 67,
        REG_D28 = 68,
        REG_D29 = 69,
        REG_D30 = 70,
        REG_D31 = 71,
        REG_H0 = 72,
        REG_H1 = 73,
        REG_H2 = 74,
        REG_H3 = 75,
        REG_H4 = 76,
        REG_H5 = 77,
        REG_H6 = 78,
        REG_H7 = 79,
        REG_H8 = 80,
        REG_H9 = 81,
        REG_H10 = 82,
        REG_H11 = 83,
        REG_H12 = 84,
        REG_H13 = 85,
        REG_H14 = 86,
        REG_H15 = 87,
        REG_H16 = 88,
        REG_H17 = 89,
        REG_H18 = 90,
        REG_H19 = 91,
        REG_H20 = 92,
        REG_H21 = 93,
        REG_H22 = 94,
        REG_H23 = 95,
        REG_H24 = 96,
        REG_H25 = 97,
        REG_H26 = 98,
        REG_H27 = 99,
        REG_H28 = 100,
        REG_H29 = 101,
        REG_H30 = 102,
        REG_H31 = 103,
        REG_Q0 = 104,
        REG_Q1 = 105,
        REG_Q2 = 106,
        REG_Q3 = 107,
        REG_Q4 = 108,
        REG_Q5 = 109,
        REG_Q6 = 110,
        REG_Q7 = 111,
        REG_Q8 = 112,
        REG_Q9 = 113,
        REG_Q10 = 114,
        REG_Q11 = 115,
        REG_Q12 = 116,
        REG_Q13 = 117,
        REG_Q14 = 118,
        REG_Q15 = 119,
        REG_Q16 = 120,
        REG_Q17 = 121,
        REG_Q18 = 122,
        REG_Q19 = 123,
        REG_Q20 = 124,
        REG_Q21 = 125,
        REG_Q22 = 126,
        REG_Q23 = 127,
        REG_Q24 = 128,
        REG_Q25 = 129,
        REG_Q26 = 130,
        REG_Q27 = 131,
        REG_Q28 = 132,
        REG_Q29 = 133,
        REG_Q30 = 134,
        REG_Q31 = 135,
        REG_S0 = 136,
        REG_S1 = 137,
        REG_S2 = 138,
        REG_S3 = 139,
        REG_S4 = 140,
        REG_S5 = 141,
        REG_S6 = 142,
        REG_S7 = 143,
        REG_S8 = 144,
        REG_S9 = 145,
        REG_S10 = 146,
        REG_S11 = 147,
        REG_S12 = 148,
        REG_S13 = 149,
        REG_S14 = 150,
        REG_S15 = 151,
        REG_S16 = 152,
        REG_S17 = 153,
        REG_S18 = 154,
        REG_S19 = 155,
        REG_S20 = 156,
        REG_S21 = 157,
        REG_S22 = 158,
        REG_S23 = 159,
        REG_S24 = 160,
        REG_S25 = 161,
        REG_S26 = 162,
        REG_S27 = 163,
        REG_S28 = 164,
        REG_S29 = 165,
        REG_S30 = 166,
        REG_S31 = 167,
        REG_W0 = 168,
        REG_W1 = 169,
        REG_W2 = 170,
        REG_W3 = 171,
        REG_W4 = 172,
        REG_W5 = 173,
        REG_W6 = 174,
        REG_W7 = 175,
        REG_W8 = 176,
        REG_W9 = 177,
        REG_W10 = 178,
        REG_W11 = 179,
        REG_W12 = 180,
        REG_W13 = 181,
        REG_W14 = 182,
        REG_W15 = 183,
        REG_W16 = 184,
        REG_W17 = 185,
        REG_W18 = 186,
        REG_W19 = 187,
        REG_W20 = 188,
        REG_W21 = 189,
        REG_W22 = 190,
        REG_W23 = 191,
        REG_W24 = 192,
        REG_W25 = 193,
        REG_W26 = 194,
        REG_W27 = 195,
        REG_W28 = 196,
        REG_W29 = 197,
        REG_W30 = 198,
        REG_X0 = 199,
        REG_X1 = 200,
        REG_X2 = 201,
        REG_X3 = 202,
        REG_X4 = 203,
        REG_X5 = 204,
        REG_X6 = 205,
        REG_X7 = 206,
        REG_X8 = 207,
        REG_X9 = 208,
        REG_X10 = 209,
        REG_X11 = 210,
        REG_X12 = 211,
        REG_X13 = 212,
        REG_X14 = 213,
        REG_X15 = 214,
        REG_X16 = 215,
        REG_X17 = 216,
        REG_X18 = 217,
        REG_X19 = 218,
        REG_X20 = 219,
        REG_X21 = 220,
        REG_X22 = 221,
        REG_X23 = 222,
        REG_X24 = 223,
        REG_X25 = 224,
        REG_X26 = 225,
        REG_X27 = 226,
        REG_X28 = 227,
        REG_V0 = 228,
        REG_V1 = 229,
        REG_V2 = 230,
        REG_V3 = 231,
        REG_V4 = 232,
        REG_V5 = 233,
        REG_V6 = 234,
        REG_V7 = 235,
        REG_V8 = 236,
        REG_V9 = 237,
        REG_V10 = 238,
        REG_V11 = 239,
        REG_V12 = 240,
        REG_V13 = 241,
        REG_V14 = 242,
        REG_V15 = 243,
        REG_V16 = 244,
        REG_V17 = 245,
        REG_V18 = 246,
        REG_V19 = 247,
        REG_V20 = 248,
        REG_V21 = 249,
        REG_V22 = 250,
        REG_V23 = 251,
        REG_V24 = 252,
        REG_V25 = 253,
        REG_V26 = 254,
        REG_V27 = 255,
        REG_V28 = 256,
        REG_V29 = 257,
        REG_V30 = 258,
        REG_V31 = 259,

        // pseudo registers
        REG_PC = 260,
        REG_CPACR_EL1 = 261,

        // thread registers, depreciated, use UC_ARM64_REG_CP_REG instead
        REG_TPIDR_EL0 = 262,
        REG_TPIDRRO_EL0 = 263,
        REG_TPIDR_EL1 = 264,
        REG_PSTATE = 265,

        // exception link registers, depreciated, use UC_ARM64_REG_CP_REG instead
        REG_ELR_EL0 = 266,
        REG_ELR_EL1 = 267,
        REG_ELR_EL2 = 268,
        REG_ELR_EL3 = 269,

        // stack pointers registers, depreciated, use UC_ARM64_REG_CP_REG instead
        REG_SP_EL0 = 270,
        REG_SP_EL1 = 271,
        REG_SP_EL2 = 272,
        REG_SP_EL3 = 273,

        // other CP15 registers, depreciated, use UC_ARM64_REG_CP_REG instead
        REG_TTBR0_EL1 = 274,
        REG_TTBR1_EL1 = 275,
        REG_ESR_EL0 = 276,
        REG_ESR_EL1 = 277,
        REG_ESR_EL2 = 278,
        REG_ESR_EL3 = 279,
        REG_FAR_EL0 = 280,
        REG_FAR_EL1 = 281,
        REG_FAR_EL2 = 282,
        REG_FAR_EL3 = 283,
        REG_PAR_EL1 = 284,
        REG_MAIR_EL1 = 285,
        REG_VBAR_EL0 = 286,
        REG_VBAR_EL1 = 287,
        REG_VBAR_EL2 = 288,
        REG_VBAR_EL3 = 289,
        REG_CP_REG = 290,

        // floating point control and status registers
        REG_FPCR = 291,
        REG_FPSR = 292,
        REG_ENDING = 293,

        // alias registers
        REG_IP0 = 215,
        REG_IP1 = 216,
        REG_FP = 1,
        REG_LR = 2,

        // ARM64 instructions

        INS_INVALID = 0,
        INS_MRS = 1,
        INS_MSR = 2,
        INS_SYS = 3,
        INS_SYSL = 4,
        INS_ENDING = 5,
    }
}
