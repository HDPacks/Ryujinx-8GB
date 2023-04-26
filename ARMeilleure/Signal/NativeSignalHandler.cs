﻿using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using ARMeilleure.Translation;
using ARMeilleure.Translation.Cache;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Signal
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SignalHandlerRange
    {
        public int IsActive;
        public nuint RangeAddress;
        public nuint RangeEndAddress;
        public IntPtr ActionPointer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SignalHandlerConfig
    {
        /// <summary>
        /// The byte offset of the faulting address in the SigInfo or ExceptionRecord struct.
        /// </summary>
        public int StructAddressOffset;

        /// <summary>
        /// The byte offset of the write flag in the SigInfo or ExceptionRecord struct.
        /// </summary>
        public int StructWriteOffset;

        /// <summary>
        /// The sigaction handler that was registered before this one. (unix only)
        /// </summary>
        public nuint UnixOldSigaction;

        /// <summary>
        /// The type of the previous sigaction. True for the 3 argument variant. (unix only)
        /// </summary>
        public int UnixOldSigaction3Arg;

        public SignalHandlerRange Range0;
        public SignalHandlerRange Range1;
        public SignalHandlerRange Range2;
        public SignalHandlerRange Range3;
        public SignalHandlerRange Range4;
        public SignalHandlerRange Range5;
        public SignalHandlerRange Range6;
        public SignalHandlerRange Range7;
    }

    public static class NativeSignalHandler
    {
        private delegate void UnixExceptionHandler(int sig, IntPtr info, IntPtr ucontext);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VectoredExceptionHandler(IntPtr exceptionInfo);

        private const int MaxTrackedRanges = 8;

        private const int StructAddressOffset = 0;
        private const int StructWriteOffset = 4;
        private const int UnixOldSigaction = 8;
        private const int UnixOldSigaction3Arg = 16;
        private const int RangeOffset = 20;

        private const int EXCEPTION_CONTINUE_SEARCH = 0;
        private const int EXCEPTION_CONTINUE_EXECUTION = -1;

        private const uint EXCEPTION_ACCESS_VIOLATION = 0xc0000005;

        private static ulong _pageSize;
        private static ulong _pageMask;

        private static IntPtr _handlerConfig;
        private static IntPtr _signalHandlerPtr;
        private static IntPtr _signalHandlerHandle;

        private static readonly object _lock = new object();
        private static bool _initialized;

        static NativeSignalHandler()
        {
            _handlerConfig = Marshal.AllocHGlobal(Unsafe.SizeOf<SignalHandlerConfig>());
            ref SignalHandlerConfig config = ref GetConfigRef();

            config = new SignalHandlerConfig();
        }

        public static void Initialize(IJitMemoryAllocator allocator)
        {
            JitCache.Initialize(allocator);
        }

        public static void InitializeSignalHandler(ulong pageSize, Func<IntPtr, IntPtr, IntPtr> customSignalHandlerFactory = null)
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                _pageSize = pageSize;
                _pageMask = pageSize - 1;

                ref SignalHandlerConfig config = ref GetConfigRef();

                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    _signalHandlerPtr = Marshal.GetFunctionPointerForDelegate(GenerateUnixSignalHandler(_handlerConfig));

                    if (customSignalHandlerFactory != null)
                    {
                        _signalHandlerPtr = customSignalHandlerFactory(UnixSignalHandlerRegistration.GetSegfaultExceptionHandler().sa_handler, _signalHandlerPtr);
                    }

                    var old = UnixSignalHandlerRegistration.RegisterExceptionHandler(_signalHandlerPtr);

                    config.UnixOldSigaction = (nuint)(ulong)old.sa_handler;
                    config.UnixOldSigaction3Arg = old.sa_flags & 4;
                }
                else
                {
                    config.StructAddressOffset = 40; // ExceptionInformation1
                    config.StructWriteOffset = 32; // ExceptionInformation0

                    _signalHandlerPtr = Marshal.GetFunctionPointerForDelegate(GenerateWindowsSignalHandler(_handlerConfig));

                    if (customSignalHandlerFactory != null)
                    {
                        _signalHandlerPtr = customSignalHandlerFactory(IntPtr.Zero, _signalHandlerPtr);
                    }

                    _signalHandlerHandle = WindowsSignalHandlerRegistration.RegisterExceptionHandler(_signalHandlerPtr);
                }

                _initialized = true;
            }
        }

        private static unsafe ref SignalHandlerConfig GetConfigRef()
        {
            return ref Unsafe.AsRef<SignalHandlerConfig>((void*)_handlerConfig);
        }

        public static unsafe bool AddTrackedRegion(nuint address, nuint endAddress, IntPtr action)
        {
            var ranges = &((SignalHandlerConfig*)_handlerConfig)->Range0;

            for (int i = 0; i < MaxTrackedRanges; i++)
            {
                if (ranges[i].IsActive == 0)
                {
                    ranges[i].RangeAddress = address;
                    ranges[i].RangeEndAddress = endAddress;
                    ranges[i].ActionPointer = action;
                    ranges[i].IsActive = 1;

                    return true;
                }
            }

            return false;
        }

        public static unsafe bool RemoveTrackedRegion(nuint address)
        {
            var ranges = &((SignalHandlerConfig*)_handlerConfig)->Range0;

            for (int i = 0; i < MaxTrackedRanges; i++)
            {
                if (ranges[i].IsActive == 1 && ranges[i].RangeAddress == address)
                {
                    ranges[i].IsActive = 0;

                    return true;
                }
            }

            return false;
        }

        private static Operand EmitGenericRegionCheck(EmitterContext context, IntPtr signalStructPtr, Operand faultAddress, Operand isWrite)
        {
            Operand inRegionLocal = context.AllocateLocal(OperandType.I32);
            context.Copy(inRegionLocal, Const(0));

            Operand endLabel = Label();

            for (int i = 0; i < MaxTrackedRanges; i++)
            {
                ulong rangeBaseOffset = (ulong)(RangeOffset + i * Unsafe.SizeOf<SignalHandlerRange>());

                Operand nextLabel = Label();

                Operand isActive = context.Load(OperandType.I32, Const((ulong)signalStructPtr + rangeBaseOffset));

                context.BranchIfFalse(nextLabel, isActive);

                Operand rangeAddress = context.Load(OperandType.I64, Const((ulong)signalStructPtr + rangeBaseOffset + 4));
                Operand rangeEndAddress = context.Load(OperandType.I64, Const((ulong)signalStructPtr + rangeBaseOffset + 12));

                // Is the fault address within this tracked region?
                Operand inRange = context.BitwiseAnd(
                    context.ICompare(faultAddress, rangeAddress, Comparison.GreaterOrEqualUI),
                    context.ICompare(faultAddress, rangeEndAddress, Comparison.LessUI)
                    );

                // Only call tracking if in range.
                context.BranchIfFalse(nextLabel, inRange, BasicBlockFrequency.Cold);

                Operand offset = context.BitwiseAnd(context.Subtract(faultAddress, rangeAddress), Const(~_pageMask));

                // Call the tracking action, with the pointer's relative offset to the base address.
                Operand trackingActionPtr = context.Load(OperandType.I64, Const((ulong)signalStructPtr + rangeBaseOffset + 20));

                context.Copy(inRegionLocal, Const(0));

                Operand skipActionLabel = Label();

                // Tracking action should be non-null to call it, otherwise assume false return.
                context.BranchIfFalse(skipActionLabel, trackingActionPtr);
                Operand result = context.Call(trackingActionPtr, OperandType.I32, offset, Const(_pageSize), isWrite);
                context.Copy(inRegionLocal, result);

                context.MarkLabel(skipActionLabel);

                // If the tracking action returns false or does not exist, it might be an invalid access due to a partial overlap on Windows.
                if (OperatingSystem.IsWindows())
                {
                    context.BranchIfTrue(endLabel, inRegionLocal);

                    context.Copy(inRegionLocal, WindowsPartialUnmapHandler.EmitRetryFromAccessViolation(context));
                }

                context.Branch(endLabel);

                context.MarkLabel(nextLabel);
            }

            context.MarkLabel(endLabel);

            return context.Copy(inRegionLocal);
        }

        private static Operand GenerateUnixFaultAddress(EmitterContext context, Operand sigInfoPtr)
        {
            ulong structAddressOffset = OperatingSystem.IsMacOS() ? 24ul : 16ul; // si_addr
            return context.Load(OperandType.I64, context.Add(sigInfoPtr, Const(structAddressOffset)));
        }

        private static Operand GenerateUnixWriteFlag(EmitterContext context, Operand ucontextPtr)
        {
            if (OperatingSystem.IsMacOS())
            {
                const ulong mcontextOffset = 48; // uc_mcontext
                Operand ctxPtr = context.Load(OperandType.I64, context.Add(ucontextPtr, Const(mcontextOffset)));

                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    const ulong esrOffset = 8; // __es.__esr
                    Operand esr = context.Load(OperandType.I64, context.Add(ctxPtr, Const(esrOffset)));
                    return context.BitwiseAnd(esr, Const(0x40ul));
                }

                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    const ulong errOffset = 4; // __es.__err
                    Operand err = context.Load(OperandType.I64, context.Add(ctxPtr, Const(errOffset)));
                    return context.BitwiseAnd(err, Const(2ul));
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    Operand auxPtr = context.AllocateLocal(OperandType.I64);

                    Operand loopLabel = Label();
                    Operand successLabel = Label();

                    const ulong auxOffset = 464; // uc_mcontext.__reserved
                    const uint esrMagic = 0x45535201;

                    context.Copy(auxPtr,  context.Add(ucontextPtr, Const(auxOffset)));

                    context.MarkLabel(loopLabel);

                    // _aarch64_ctx::magic
                    Operand magic = context.Load(OperandType.I32, auxPtr);
                    // _aarch64_ctx::size
                    Operand size = context.Load(OperandType.I32, context.Add(auxPtr, Const(4ul)));

                    context.BranchIf(successLabel, magic, Const(esrMagic), Comparison.Equal);

                    context.Copy(auxPtr, context.Add(auxPtr, context.ZeroExtend32(OperandType.I64, size)));

                    context.Branch(loopLabel);

                    context.MarkLabel(successLabel);

                    // esr_context::esr
                    Operand esr = context.Load(OperandType.I64, context.Add(auxPtr, Const(8ul)));
                    return context.BitwiseAnd(esr, Const(0x40ul));
                }

                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    const int errOffset = 192; // uc_mcontext.gregs[REG_ERR]
                    Operand err = context.Load(OperandType.I64, context.Add(ucontextPtr, Const(errOffset)));
                    return context.BitwiseAnd(err, Const(2ul));
                }
            }

            throw new PlatformNotSupportedException();
        }

        private static UnixExceptionHandler GenerateUnixSignalHandler(IntPtr signalStructPtr)
        {
            EmitterContext context = new EmitterContext();

            // (int sig, SigInfo* sigInfo, void* ucontext)
            Operand sigInfoPtr = context.LoadArgument(OperandType.I64, 1);
            Operand ucontextPtr = context.LoadArgument(OperandType.I64, 2);

            Operand faultAddress = GenerateUnixFaultAddress(context, sigInfoPtr);
            Operand writeFlag = GenerateUnixWriteFlag(context, ucontextPtr);

            Operand isWrite = context.ICompareNotEqual(writeFlag, Const(0L)); // Normalize to 0/1.

            Operand isInRegion = EmitGenericRegionCheck(context, signalStructPtr, faultAddress, isWrite);

            Operand endLabel = Label();

            context.BranchIfTrue(endLabel, isInRegion);

            Operand unixOldSigaction = context.Load(OperandType.I64, Const((ulong)signalStructPtr + UnixOldSigaction));
            Operand unixOldSigaction3Arg = context.Load(OperandType.I64, Const((ulong)signalStructPtr + UnixOldSigaction3Arg));
            Operand threeArgLabel = Label();

            context.BranchIfTrue(threeArgLabel, unixOldSigaction3Arg);

            context.Call(unixOldSigaction, OperandType.None, context.LoadArgument(OperandType.I32, 0));
            context.Branch(endLabel);

            context.MarkLabel(threeArgLabel);

            context.Call(unixOldSigaction,
                OperandType.None,
                context.LoadArgument(OperandType.I32, 0),
                sigInfoPtr,
                context.LoadArgument(OperandType.I64, 2)
                );

            context.MarkLabel(endLabel);

            context.Return();

            ControlFlowGraph cfg = context.GetControlFlowGraph();

            OperandType[] argTypes = new OperandType[] { OperandType.I32, OperandType.I64, OperandType.I64 };

            return Compiler.Compile(cfg, argTypes, OperandType.None, CompilerOptions.HighCq, RuntimeInformation.ProcessArchitecture).Map<UnixExceptionHandler>();
        }

        private static VectoredExceptionHandler GenerateWindowsSignalHandler(IntPtr signalStructPtr)
        {
            EmitterContext context = new EmitterContext();

            // (ExceptionPointers* exceptionInfo)
            Operand exceptionInfoPtr = context.LoadArgument(OperandType.I64, 0);
            Operand exceptionRecordPtr = context.Load(OperandType.I64, exceptionInfoPtr);

            // First thing's first - this catches a number of exceptions, but we only want access violations.
            Operand validExceptionLabel = Label();

            Operand exceptionCode = context.Load(OperandType.I32, exceptionRecordPtr);

            context.BranchIf(validExceptionLabel, exceptionCode, Const(EXCEPTION_ACCESS_VIOLATION), Comparison.Equal);

            context.Return(Const(EXCEPTION_CONTINUE_SEARCH)); // Don't handle this one.

            context.MarkLabel(validExceptionLabel);

            // Next, read the address of the invalid access, and whether it is a write or not.

            Operand structAddressOffset = context.Load(OperandType.I32, Const((ulong)signalStructPtr + StructAddressOffset));
            Operand structWriteOffset = context.Load(OperandType.I32, Const((ulong)signalStructPtr + StructWriteOffset));

            Operand faultAddress = context.Load(OperandType.I64, context.Add(exceptionRecordPtr, context.ZeroExtend32(OperandType.I64, structAddressOffset)));
            Operand writeFlag = context.Load(OperandType.I64, context.Add(exceptionRecordPtr, context.ZeroExtend32(OperandType.I64, structWriteOffset)));

            Operand isWrite = context.ICompareNotEqual(writeFlag, Const(0L)); // Normalize to 0/1.

            Operand isInRegion = EmitGenericRegionCheck(context, signalStructPtr, faultAddress, isWrite);

            Operand endLabel = Label();

            // If the region check result is false, then run the next vectored exception handler.

            context.BranchIfTrue(endLabel, isInRegion);

            context.Return(Const(EXCEPTION_CONTINUE_SEARCH));

            context.MarkLabel(endLabel);

            // Otherwise, return to execution.

            context.Return(Const(EXCEPTION_CONTINUE_EXECUTION));

            // Compile and return the function.

            ControlFlowGraph cfg = context.GetControlFlowGraph();

            OperandType[] argTypes = new OperandType[] { OperandType.I64 };

            return Compiler.Compile(cfg, argTypes, OperandType.I32, CompilerOptions.HighCq, RuntimeInformation.ProcessArchitecture).Map<VectoredExceptionHandler>();
        }
    }
}
