﻿using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan.Queries
{
    class BufferedQuery : IDisposable
    {
        private const int MaxQueryRetries = 5000;
        private const long DefaultValue = -1;
        private const long DefaultValueInt = 0xFFFFFFFF;

        private readonly Vk _api;
        private readonly Device _device;
        private readonly PipelineFull _pipeline;

        private QueryPool _queryPool;
        private bool _isReset;

        private readonly BufferHolder _buffer;
        private readonly IntPtr _bufferMap;
        private readonly CounterType _type;
        private bool _result32Bit;
        private bool _isSupported;

        private long _defaultValue;

        public unsafe BufferedQuery(VulkanRenderer gd, Device device, PipelineFull pipeline, CounterType type, bool result32Bit)
        {
            _api = gd.Api;
            _device = device;
            _pipeline = pipeline;
            _type = type;
            _result32Bit = result32Bit;

            _isSupported = QueryTypeSupported(gd, type);

            if (_isSupported)
            {
                QueryPipelineStatisticFlags flags = type == CounterType.PrimitivesGenerated ?
                    QueryPipelineStatisticFlags.GeometryShaderPrimitivesBit : 0;

                var queryPoolCreateInfo = new QueryPoolCreateInfo()
                {
                    SType = StructureType.QueryPoolCreateInfo,
                    QueryCount = 1,
                    QueryType = GetQueryType(type),
                    PipelineStatistics = flags
                };

                gd.Api.CreateQueryPool(device, queryPoolCreateInfo, null, out _queryPool).ThrowOnError();
            }

            var buffer = gd.BufferManager.Create(gd, sizeof(long), forConditionalRendering: true);

            _bufferMap = buffer.Map(0, sizeof(long));
            _defaultValue = result32Bit ? DefaultValueInt : DefaultValue;
            Marshal.WriteInt64(_bufferMap, _defaultValue);
            _buffer = buffer;
        }

        private bool QueryTypeSupported(VulkanRenderer gd, CounterType type)
        {
            return type switch
            {
                CounterType.SamplesPassed => true,
                CounterType.TransformFeedbackPrimitivesWritten => gd.Capabilities.SupportsTransformFeedbackQueries,
                CounterType.PrimitivesGenerated => gd.Capabilities.SupportsGeometryShader,
                _ => false
            };
        }

        private static QueryType GetQueryType(CounterType type)
        {
            return type switch
            {
                CounterType.SamplesPassed => QueryType.Occlusion,
                CounterType.PrimitivesGenerated => QueryType.PipelineStatistics,
                CounterType.TransformFeedbackPrimitivesWritten => QueryType.TransformFeedbackStreamExt,
                _ => QueryType.Occlusion
            };
        }

        public Auto<DisposableBuffer> GetBuffer()
        {
            return _buffer.GetBuffer();
        }

        public void Reset()
        {
            End(false);
            Begin();
        }

        public void Begin()
        {
            if (_isSupported)
            {
                _pipeline.BeginQuery(this, _queryPool, !_isReset);
            }
            _isReset = false;
        }

        public unsafe void End(bool withResult)
        {
            if (_isSupported)
            {
                _pipeline.EndQuery(_queryPool);
            }

            if (withResult && _isSupported)
            {
                Marshal.WriteInt64(_bufferMap, _defaultValue);
                _pipeline.CopyQueryResults(this);
            }
            else
            {
                // Dummy result, just return 0.
                Marshal.WriteInt64(_bufferMap, 0);
            }
        }

        public bool TryGetResult(out long result)
        {
            result = Marshal.ReadInt64(_bufferMap);

            return result != _defaultValue;
        }

        public long AwaitResult(AutoResetEvent wakeSignal = null)
        {
            long data = _defaultValue;

            if (wakeSignal == null)
            {
                while (data == _defaultValue)
                {
                    data = Marshal.ReadInt64(_bufferMap);
                }
            }
            else
            {
                int iterations = 0;
                while (data == _defaultValue && iterations++ < MaxQueryRetries)
                {
                    data = Marshal.ReadInt64(_bufferMap);
                    if (data == _defaultValue)
                    {
                        wakeSignal.WaitOne(1);
                    }
                }

                if (iterations >= MaxQueryRetries)
                {
                    Logger.Error?.Print(LogClass.Gpu, $"Error: Query result {_type} timed out. Took more than {MaxQueryRetries} tries.");
                }
            }

            return data;
        }

        public void PoolReset(CommandBuffer cmd)
        {
            if (_isSupported)
            {
                _api.CmdResetQueryPool(cmd, _queryPool, 0, 1);
            }
            _isReset = true;
        }

        public void PoolCopy(CommandBufferScoped cbs)
        {
            var buffer = _buffer.GetBuffer(cbs.CommandBuffer, true).Get(cbs, 0, sizeof(long)).Value;

            QueryResultFlags flags = QueryResultFlags.ResultWaitBit;

            if (!_result32Bit)
            {
                flags |= QueryResultFlags.Result64Bit;
            }

            _api.CmdCopyQueryPoolResults(
                cbs.CommandBuffer,
                _queryPool,
                0,
                1,
                buffer,
                0,
                (ulong)(_result32Bit ? sizeof(int) : sizeof(long)),
                flags);
        }

        public unsafe void Dispose()
        {
            _buffer.Dispose();
            if (_isSupported)
            {
                _api.DestroyQueryPool(_device, _queryPool, null);
            }
            _queryPool = default;
        }
    }
}
