﻿using Silk.NET.Vulkan;
using System;

namespace Ryujinx.Graphics.Vulkan
{
    [Flags]
    enum PortabilitySubsetFlags
    {
        None = 0,

        NoTriangleFans = 1,
        NoPointMode = 1 << 1,
        No3DImageView = 1 << 2,
        NoLodBias = 1 << 3
    }

    readonly struct HardwareCapabilities
    {
        public readonly bool SupportsIndexTypeUint8;
        public readonly bool SupportsCustomBorderColor;
        public readonly bool SupportsBlendEquationAdvanced;
        public readonly bool SupportsBlendEquationAdvancedCorrelatedOverlap;
        public readonly bool SupportsBlendEquationAdvancedNonPreMultipliedSrcColor;
        public readonly bool SupportsBlendEquationAdvancedNonPreMultipliedDstColor;
        public readonly bool SupportsIndirectParameters;
        public readonly bool SupportsFragmentShaderInterlock;
        public readonly bool SupportsGeometryShaderPassthrough;
        public readonly bool SupportsSubgroupSizeControl;
        public readonly bool SupportsShaderInt8;
        public readonly bool SupportsShaderStencilExport;
        public readonly bool SupportsConditionalRendering;
        public readonly bool SupportsExtendedDynamicState;
        public readonly bool SupportsMultiView;
        public readonly bool SupportsNullDescriptors;
        public readonly bool SupportsPushDescriptors;
        public readonly bool SupportsPrimitiveTopologyListRestart;
        public readonly bool SupportsPrimitiveTopologyPatchListRestart;
        public readonly bool SupportsTransformFeedback;
        public readonly bool SupportsTransformFeedbackQueries;
        public readonly bool SupportsPreciseOcclusionQueries;
        public readonly bool SupportsPipelineStatisticsQuery;
        public readonly bool SupportsGeometryShader;
        public readonly uint MinSubgroupSize;
        public readonly uint MaxSubgroupSize;
        public readonly ShaderStageFlags RequiredSubgroupSizeStages;
        public readonly SampleCountFlags SupportedSampleCounts;
        public readonly PortabilitySubsetFlags PortabilitySubset;
        public readonly uint VertexBufferAlignment;

        public HardwareCapabilities(
            bool supportsIndexTypeUint8,
            bool supportsCustomBorderColor,
            bool supportsBlendEquationAdvanced,
            bool supportsBlendEquationAdvancedCorrelatedOverlap,
            bool supportsBlendEquationAdvancedNonPreMultipliedSrcColor,
            bool supportsBlendEquationAdvancedNonPreMultipliedDstColor,
            bool supportsIndirectParameters,
            bool supportsFragmentShaderInterlock,
            bool supportsGeometryShaderPassthrough,
            bool supportsSubgroupSizeControl,
            bool supportsShaderInt8,
            bool supportsShaderStencilExport,
            bool supportsConditionalRendering,
            bool supportsExtendedDynamicState,
            bool supportsMultiView,
            bool supportsNullDescriptors,
            bool supportsPushDescriptors,
            bool supportsPrimitiveTopologyListRestart,
            bool supportsPrimitiveTopologyPatchListRestart,
            bool supportsTransformFeedback,
            bool supportsTransformFeedbackQueries,
            bool supportsPreciseOcclusionQueries,
            bool supportsPipelineStatisticsQuery,
            bool supportsGeometryShader,
            uint minSubgroupSize,
            uint maxSubgroupSize,
            ShaderStageFlags requiredSubgroupSizeStages,
            SampleCountFlags supportedSampleCounts,
            PortabilitySubsetFlags portabilitySubset,
            uint vertexBufferAlignment)
        {
            SupportsIndexTypeUint8 = supportsIndexTypeUint8;
            SupportsCustomBorderColor = supportsCustomBorderColor;
            SupportsBlendEquationAdvanced = supportsBlendEquationAdvanced;
            SupportsBlendEquationAdvancedCorrelatedOverlap = supportsBlendEquationAdvancedCorrelatedOverlap;
            SupportsBlendEquationAdvancedNonPreMultipliedSrcColor = supportsBlendEquationAdvancedNonPreMultipliedSrcColor;
            SupportsBlendEquationAdvancedNonPreMultipliedDstColor = supportsBlendEquationAdvancedNonPreMultipliedDstColor;
            SupportsIndirectParameters = supportsIndirectParameters;
            SupportsFragmentShaderInterlock = supportsFragmentShaderInterlock;
            SupportsGeometryShaderPassthrough = supportsGeometryShaderPassthrough;
            SupportsSubgroupSizeControl = supportsSubgroupSizeControl;
            SupportsShaderInt8 = supportsShaderInt8;
            SupportsShaderStencilExport = supportsShaderStencilExport;
            SupportsConditionalRendering = supportsConditionalRendering;
            SupportsExtendedDynamicState = supportsExtendedDynamicState;
            SupportsMultiView = supportsMultiView;
            SupportsNullDescriptors = supportsNullDescriptors;
            SupportsPushDescriptors = supportsPushDescriptors;
            SupportsPrimitiveTopologyListRestart = supportsPrimitiveTopologyListRestart;
            SupportsPrimitiveTopologyPatchListRestart = supportsPrimitiveTopologyPatchListRestart;
            SupportsTransformFeedback = supportsTransformFeedback;
            SupportsTransformFeedbackQueries = supportsTransformFeedbackQueries;
            SupportsPreciseOcclusionQueries = supportsPreciseOcclusionQueries;
            SupportsPipelineStatisticsQuery = supportsPipelineStatisticsQuery;
            SupportsGeometryShader = supportsGeometryShader;
            MinSubgroupSize = minSubgroupSize;
            MaxSubgroupSize = maxSubgroupSize;
            RequiredSubgroupSizeStages = requiredSubgroupSizeStages;
            SupportedSampleCounts = supportedSampleCounts;
            PortabilitySubset = portabilitySubset;
            VertexBufferAlignment = vertexBufferAlignment;
        }
    }
}
