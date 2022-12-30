using Ryujinx.Audio.Renderer.Dsp.Effect;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System;

namespace Ryujinx.Audio.Renderer.Dsp.State
{
    public class LimiterState
    {
        public ExponentialMovingAverage[] DetectorAverage;
        public ExponentialMovingAverage[] CompressionGainAverage;
        public float[] DelayedSampleBuffer;
        public int[] DelayedSampleBufferPosition;

        public LimiterState(ref LimiterParameter parameter, ulong workBuffer)
        {
            DetectorAverage = new ExponentialMovingAverage[parameter.ChannelCount];
            CompressionGainAverage = new ExponentialMovingAverage[parameter.ChannelCount];
            DelayedSampleBuffer = new float[parameter.ChannelCount * parameter.DelayBufferSampleCountMax];
            DelayedSampleBufferPosition = new int[parameter.ChannelCount];

            DetectorAverage.AsSpan().Fill(new ExponentialMovingAverage(0.0f));
            CompressionGainAverage.AsSpan().Fill(new ExponentialMovingAverage(1.0f));
            DelayedSampleBufferPosition.AsSpan().Fill(0);
            DelayedSampleBuffer.AsSpan().Fill(0.0f);

            UpdateParameter(ref parameter);
        }

        public void UpdateParameter(ref LimiterParameter parameter) { }
    }
}