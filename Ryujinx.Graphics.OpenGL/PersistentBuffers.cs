﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;

namespace Ryujinx.Graphics.OpenGL
{
    class PersistentBuffers : IDisposable
    {
        private PersistentBuffer _main = new PersistentBuffer();
        private PersistentBuffer _background = new PersistentBuffer();

        public PersistentBuffer Default => BackgroundContextWorker.InBackground ? _background : _main;

        public void Dispose()
        {
            _main?.Dispose();
            _background?.Dispose();
        }
    }

    class PersistentBuffer : IDisposable
    {
        private IntPtr _bufferMap;
        private int _copyBufferHandle;
        private int _copyBufferSize;

        private byte[] _data;
        private IntPtr _dataMap;

        private void EnsureBuffer(int requiredSize)
        {
            if (_copyBufferSize < requiredSize && _copyBufferHandle != 0)
            {
                GL.DeleteBuffer(_copyBufferHandle);

                _copyBufferHandle = 0;
            }

            if (_copyBufferHandle == 0)
            {
                _copyBufferHandle = GL.GenBuffer();
                _copyBufferSize = requiredSize;

                GL.BindBuffer(BufferTarget.CopyWriteBuffer, _copyBufferHandle);
                GL.BufferStorage(BufferTarget.CopyWriteBuffer, requiredSize, IntPtr.Zero, BufferStorageFlags.MapReadBit | BufferStorageFlags.MapPersistentBit);

                _bufferMap = GL.MapBufferRange(BufferTarget.CopyWriteBuffer, IntPtr.Zero, requiredSize, BufferAccessMask.MapReadBit | BufferAccessMask.MapPersistentBit);
            }
        }

        public unsafe IntPtr GetHostArray(int requiredSize)
        {
            if (_data == null || _data.Length < requiredSize)
            {
                _data = GC.AllocateUninitializedArray<byte>(requiredSize, true);

                _dataMap = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_data));
            }

            return _dataMap;
        }

        private void Sync()
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ClientMappedBufferBarrierBit);

            IntPtr sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            WaitSyncStatus syncResult = GL.ClientWaitSync(sync, ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000000);

            if (syncResult == WaitSyncStatus.TimeoutExpired)
            {
                Logger.Error?.PrintMsg(LogClass.Gpu, $"Failed to sync persistent buffer state within 1000ms. Continuing...");
            }

            GL.DeleteSync(sync);
        }

        public unsafe ReadOnlySpan<byte> GetTextureData(TextureView view, int size)
        {
            EnsureBuffer(size);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _copyBufferHandle);

            view.WriteToPbo(0, false);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size);
        }

        public unsafe ReadOnlySpan<byte> GetTextureData(TextureView view, int size, int layer, int level)
        {
            EnsureBuffer(size);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _copyBufferHandle);

            int offset = view.WriteToPbo2D(0, layer, level);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size).Slice(offset);
        }

        public unsafe ReadOnlySpan<byte> GetBufferData(BufferHandle buffer, int offset, int size)
        {
            EnsureBuffer(size);

            GL.BindBuffer(BufferTarget.CopyReadBuffer, buffer.ToInt32());
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _copyBufferHandle);

            GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, (IntPtr)offset, IntPtr.Zero, size);

            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size);
        }

        public void Dispose()
        {
            if (_copyBufferHandle != 0)
            {
                GL.DeleteBuffer(_copyBufferHandle);
            }
        }
    }
}
