﻿using System;

namespace Ryujinx.Horizon.Common
{
    public struct Result : IEquatable<Result>
    {
        private const int ModuleBits = 9;
        private const int DescriptionBits = 13;
        private const int ModuleMax = 1 << ModuleBits;
        private const int DescriptionMax = 1 << DescriptionBits;

        public static Result Success { get; } = new Result(0, 0);

        public int ErrorCode { get; }

        public bool IsSuccess => ErrorCode == 0;
        public bool IsFailure => ErrorCode != 0;

        public int Module => ErrorCode & (ModuleMax - 1);
        public int Description => (ErrorCode >> ModuleBits) & (DescriptionMax - 1);

        public string PrintableResult => $"{2000 + Module:D4}-{Description:D4}";

        public Result(int module, int description)
        {
            if ((uint)module >= ModuleMax)
            {
                throw new ArgumentOutOfRangeException(nameof(module));
            }

            if ((uint)description >= DescriptionMax)
            {
                throw new ArgumentOutOfRangeException(nameof(description));
            }

            ErrorCode = module | (description << ModuleBits);
        }

        public override bool Equals(object obj)
        {
            return obj is Result result && result.Equals(this);
        }

        public bool Equals(Result other)
        {
            return other.ErrorCode == ErrorCode;
        }

        public override int GetHashCode()
        {
            return ErrorCode;
        }

        public static bool operator ==(Result lhs, Result rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Result lhs, Result rhs)
        {
            return !lhs.Equals(rhs);
        }

        public bool InRange(int minInclusive, int maxInclusive)
        {
            return (uint)(Description - minInclusive) <= (uint)(maxInclusive - minInclusive);
        }

        public void AbortOnSuccess()
        {
            if (IsSuccess)
            {
                ThrowInvalidResult();
            }
        }

        public void AbortOnFailure()
        {
            if (this == KernelResult.ThreadTerminating)
            {
                throw new ThreadTerminatedException();
            }

            AbortUnless(Success);
        }

        public void AbortUnless(Result result)
        {
            if (this != result)
            {
                ThrowInvalidResult();
            }
        }

        public void AbortUnless(Result result, Result result2)
        {
            if (this != result && this != result2)
            {
                ThrowInvalidResult();
            }
        }

        private void ThrowInvalidResult()
        {
            throw new InvalidResultException(this);
        }

        public override string ToString()
        {
            if (ResultNames.TryGet(ErrorCode, out string name))
            {
                return name;
            }

            return PrintableResult;
        }
    }
}