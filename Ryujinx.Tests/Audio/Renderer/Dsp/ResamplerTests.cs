﻿using NUnit.Framework;
using Ryujinx.Audio.Renderer.Dsp;
using Ryujinx.Audio.Renderer.Parameter;
using Ryujinx.Audio.Renderer.Server.Upsampler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Tests.Audio.Renderer.Dsp
{
    class ResamplerTests
    {
        [Test]
        [TestCase(VoiceInParameter.SampleRateConversionQuality.Low)]
        [TestCase(VoiceInParameter.SampleRateConversionQuality.Default)]
        [TestCase(VoiceInParameter.SampleRateConversionQuality.High)]
        public void TestResamplerConsistencyUpsampling(VoiceInParameter.SampleRateConversionQuality quality)
        {
            DoResamplingTest(44100, 48000, quality);
        }

        [Test]
        [TestCase(VoiceInParameter.SampleRateConversionQuality.Low)]
        [TestCase(VoiceInParameter.SampleRateConversionQuality.Default)]
        [TestCase(VoiceInParameter.SampleRateConversionQuality.High)]
        public void TestResamplerConsistencyDownsampling(VoiceInParameter.SampleRateConversionQuality quality)
        {
            DoResamplingTest(48000, 44100, quality);
        }

        /// <summary>
        /// Generates a 1-second sine wave sample at input rate, resamples it to output rate, and
        /// ensures that it resampled at the expected rate with no discontinuities
        /// </summary>
        /// <param name="inputRate">The input sample rate to test</param>
        /// <param name="outputRate">The output sample rate to test</param>
        /// <param name="quality">The resampler quality to use</param>
        private static void DoResamplingTest(int inputRate, int outputRate, VoiceInParameter.SampleRateConversionQuality quality)
        {
            float inputSampleRate = (float)inputRate;
            float outputSampleRate = (float)outputRate;
            int inputSampleCount = inputRate;
            int outputSampleCount = outputRate;
            short[] inputBuffer = new short[inputSampleCount + 100]; // add some safety buffer at the end
            float[] outputBuffer = new float[outputSampleCount + 100];
            for (int sample = 0; sample < inputBuffer.Length; sample++)
            {
                // 440 hz sine wave with amplitude = 0.5f at input sample rate
                inputBuffer[sample] = (short)(32767 * MathF.Sin((440 / inputSampleRate) * (float)sample * MathF.PI * 2f) * 0.5f);
            }

            float fraction = 0;

            ResamplerHelper.Resample(
                outputBuffer.AsSpan(),
                inputBuffer.AsSpan(),
                inputSampleRate / outputSampleRate,
                ref fraction,
                outputSampleCount,
                quality,
                false);

            float[] expectedOutput = new float[outputSampleCount];
            float sumDifference = 0;
            int delay = quality switch
            {
                VoiceInParameter.SampleRateConversionQuality.High => 3,
                VoiceInParameter.SampleRateConversionQuality.Default => 1,
                _ => 0
            };

            for (int sample = 0; sample < outputSampleCount; sample++)
            {
                outputBuffer[sample] /= 32767;
                // 440 hz sine wave with amplitude = 0.5f at output sample rate
                expectedOutput[sample] = MathF.Sin((440 / outputSampleRate) * (float)(sample + delay) * MathF.PI * 2f) * 0.5f;
                float thisDelta = Math.Abs(expectedOutput[sample] - outputBuffer[sample]);

                // Ensure no discontinuities
                Assert.IsTrue(thisDelta < 0.1f);
                sumDifference += thisDelta;
            }

            sumDifference = sumDifference / (float)outputSampleCount;
            // Expect the output to be 99% similar to the expected resampled sine wave
            Assert.IsTrue(sumDifference < 0.01f);
        }
    }
}
