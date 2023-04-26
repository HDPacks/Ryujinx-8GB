﻿using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sm
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ServiceName
    {
        public static ServiceName Invalid { get; } = new ServiceName(0);

        public bool IsValid => Packed != 0;

        public int Length => sizeof(ulong);

        public ulong Packed { get; }

        public byte this[int index]
        {
            get
            {
                if ((uint)index >= sizeof(ulong))
                {
                    throw new IndexOutOfRangeException();
                }

                return (byte)(Packed >> (index * 8));
            }
        }

        private ServiceName(ulong packed)
        {
            Packed = packed;
        }

        public static ServiceName Encode(string name)
        {
            ulong packed = 0;

            for (int index = 0; index < sizeof(ulong); index++)
            {
                if (index < name.Length)
                {
                    packed |= (ulong)(byte)name[index] << (index * 8);
                }
                else
                {
                    break;
                }
            }

            return new ServiceName(packed);
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceName serviceName && serviceName.Equals(this);
        }

        public bool Equals(ServiceName other)
        {
            return other.Packed == Packed;
        }

        public override int GetHashCode()
        {
            return Packed.GetHashCode();
        }

        public static bool operator ==(ServiceName lhs, ServiceName rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ServiceName lhs, ServiceName rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override string ToString()
        {
            string name = string.Empty;

            for (int index = 0; index < sizeof(ulong); index++)
            {
                byte character = (byte)(Packed >> (index * 8));

                if (character == 0)
                {
                    break;
                }

                name += (char)character;
            }

            return name;
        }
    }
}
