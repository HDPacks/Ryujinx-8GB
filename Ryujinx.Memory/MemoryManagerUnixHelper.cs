﻿using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Memory
{
    public static partial class MemoryManagerUnixHelper
    {
        [Flags]
        public enum MmapProts : uint
        {
            PROT_NONE = 0,
            PROT_READ = 1,
            PROT_WRITE = 2,
            PROT_EXEC = 4
        }

        [Flags]
        public enum MmapFlags : uint
        {
            MAP_SHARED = 1,
            MAP_PRIVATE = 2,
            MAP_ANONYMOUS = 4,
            MAP_NORESERVE = 8,
            MAP_FIXED = 16,
            MAP_UNLOCKED = 32,
            MAP_JIT_DARWIN = 0x800
        }

        [Flags]
        public enum OpenFlags : uint
        {
            O_RDONLY = 0,
            O_WRONLY = 1,
            O_RDWR = 2,
            O_CREAT = 4,
            O_EXCL = 8,
            O_NOCTTY = 16,
            O_TRUNC = 32,
            O_APPEND = 64,
            O_NONBLOCK = 128,
            O_SYNC = 256,
        }

        private const int MAP_ANONYMOUS_LINUX_GENERIC = 0x20;
        private const int MAP_NORESERVE_LINUX_GENERIC = 0x4000;
        private const int MAP_UNLOCKED_LINUX_GENERIC = 0x80000;

        private const int MAP_NORESERVE_DARWIN = 0x40;
        private const int MAP_ANONYMOUS_DARWIN = 0x1000;

        public const int MADV_DONTNEED = 4;
        public const int MADV_REMOVE = 9;

        [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
        private static partial IntPtr Internal_mmap(IntPtr address, ulong length, MmapProts prot, int flags, int fd, long offset);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int mprotect(IntPtr address, ulong length, MmapProts prot);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int munmap(IntPtr address, ulong length);

        [LibraryImport("libc", SetLastError = true)]
        public static partial IntPtr mremap(IntPtr old_address, ulong old_size, ulong new_size, int flags, IntPtr new_address);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int madvise(IntPtr address, ulong size, int advice);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int mkstemp(IntPtr template);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int unlink(IntPtr pathname);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int ftruncate(int fildes, IntPtr length);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int close(int fd);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int shm_open(IntPtr name, int oflag, uint mode);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int shm_unlink(IntPtr name);

        private static int MmapFlagsToSystemFlags(MmapFlags flags)
        {
            int result = 0;

            if (flags.HasFlag(MmapFlags.MAP_SHARED))
            {
                result |= (int)MmapFlags.MAP_SHARED;
            }

            if (flags.HasFlag(MmapFlags.MAP_PRIVATE))
            {
                result |= (int)MmapFlags.MAP_PRIVATE;
            }

            if (flags.HasFlag(MmapFlags.MAP_FIXED))
            {
                result |= (int)MmapFlags.MAP_FIXED;
            }

            if (flags.HasFlag(MmapFlags.MAP_ANONYMOUS))
            {
                if (OperatingSystem.IsLinux())
                {
                    result |= MAP_ANONYMOUS_LINUX_GENERIC;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    result |= MAP_ANONYMOUS_DARWIN;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (flags.HasFlag(MmapFlags.MAP_NORESERVE))
            {
                if (OperatingSystem.IsLinux())
                {
                    result |= MAP_NORESERVE_LINUX_GENERIC;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    result |= MAP_NORESERVE_DARWIN;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (flags.HasFlag(MmapFlags.MAP_UNLOCKED))
            {
                if (OperatingSystem.IsLinux())
                {
                    result |= MAP_UNLOCKED_LINUX_GENERIC;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // FIXME: Doesn't exist on Darwin
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (flags.HasFlag(MmapFlags.MAP_JIT_DARWIN) && OperatingSystem.IsMacOSVersionAtLeast(10, 14))
            {
                result |= (int)MmapFlags.MAP_JIT_DARWIN;
            }

            return result;
        }

        public static IntPtr mmap(IntPtr address, ulong length, MmapProts prot, MmapFlags flags, int fd, long offset)
        {
            return Internal_mmap(address, length, prot, MmapFlagsToSystemFlags(flags), fd, offset);
        }
    }
}