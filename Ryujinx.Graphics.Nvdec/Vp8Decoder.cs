﻿using Ryujinx.Graphics.Nvdec.FFmpeg.Vp8;
using Ryujinx.Graphics.Nvdec.Image;
using Ryujinx.Graphics.Nvdec.Types.Vp8;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec
{
    static class Vp8Decoder
    {
        public static void Decode(NvdecDecoderContext context, ResourceManager rm, ref NvdecRegisters state)
        {
            PictureInfo pictureInfo = rm.Gmm.DeviceRead<PictureInfo>(state.SetPictureInfoOffset);
            ReadOnlySpan<byte> bitstream = rm.Gmm.DeviceGetSpan(state.SetBitstreamOffset, (int)pictureInfo.VLDBufferSize);

            Decoder decoder = context.GetVp8Decoder();

            ISurface outputSurface = rm.Cache.Get(decoder, 0, 0, pictureInfo.FrameWidth, pictureInfo.FrameHeight);

            Vp8PictureInfo info = pictureInfo.Convert();

            uint lumaOffset = state.SetSurfaceLumaOffset[3];
            uint chromaOffset = state.SetSurfaceChromaOffset[3];

            if (decoder.Decode(ref info, outputSurface, bitstream))
            {
                SurfaceWriter.Write(rm.Gmm, outputSurface, lumaOffset, chromaOffset);
            }

            rm.Cache.Put(outputSurface);
        }
    }
}