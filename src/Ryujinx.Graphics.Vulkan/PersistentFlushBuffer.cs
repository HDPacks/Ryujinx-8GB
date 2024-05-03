using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    internal class PersistentFlushBuffer : IDisposable
    {
        private readonly VulkanRenderer _gd;

        private BufferHolder _flushStorage;

        public PersistentFlushBuffer(VulkanRenderer gd)
        {
            _gd = gd;
        }

        private BufferHolder ResizeIfNeeded(int size)
        {
            var flushStorage = _flushStorage;

            if (flushStorage == null || size > _flushStorage.Size)
            {
                flushStorage?.Dispose();

                flushStorage = _gd.BufferManager.Create(_gd, size);
                _flushStorage = flushStorage;
            }

            return flushStorage;
        }

        public Span<byte> GetBufferData(CommandBufferPool cbp, BufferHolder buffer, int offset, int size)
        {
            var flushStorage = ResizeIfNeeded(size);
            Auto<DisposableBuffer> srcBuffer;

            using (var cbs = cbp.Rent())
            {
                srcBuffer = buffer.GetBuffer(cbs.CommandBuffer);
                var dstBuffer = flushStorage.GetBuffer(cbs.CommandBuffer);

                if (srcBuffer.TryIncrementReferenceCount())
                {
                    BufferHolder.Copy(_gd, cbs, srcBuffer, dstBuffer, offset, 0, size, registerSrcUsage: false);
                }
                else
                {
                    // Source buffer is no longer alive, don't copy anything to flush storage.
                    srcBuffer = null;
                }
            }

            flushStorage.WaitForFences();
            srcBuffer?.DecrementReferenceCount();
            return flushStorage.GetDataStorage(0, size);
        }

        public Span<byte> GetTextureData(CommandBufferPool cbp, TextureView view, int size)
        {
            TextureCreateInfo info = view.Info;

            var flushStorage = ResizeIfNeeded(size);

            using (var cbs = cbp.Rent())
            {
                var buffer = flushStorage.GetBuffer(cbs.CommandBuffer).Get(cbs).Value;
                var image = view.GetImage().Get(cbs).Value;

                view.CopyFromOrToBuffer(cbs.CommandBuffer, buffer, image, size, true, 0, 0, info.GetLayers(), info.Levels, singleSlice: false);
            }

            flushStorage.WaitForFences();
            return flushStorage.GetDataStorage(0, size);
        }

        public Span<byte> GetTextureData(CommandBufferPool cbp, TextureView view, int size, int layer, int level)
        {
            var flushStorage = ResizeIfNeeded(size);

            using (var cbs = cbp.Rent())
            {
                var buffer = flushStorage.GetBuffer(cbs.CommandBuffer).Get(cbs).Value;
                var image = view.GetImage().Get(cbs).Value;

                view.CopyFromOrToBuffer(cbs.CommandBuffer, buffer, image, size, true, layer, level, 1, 1, singleSlice: true);
            }

            flushStorage.WaitForFences();
            return flushStorage.GetDataStorage(0, size);
        }

        public void Dispose()
        {
            _flushStorage.Dispose();
        }
    }
}
