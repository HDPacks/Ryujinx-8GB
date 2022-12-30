﻿using Ryujinx.Common;
using Ryujinx.HLE.HOS.Services.Caps.Types;

namespace Ryujinx.HLE.HOS.Services.Caps
{
    [Service("caps:su")] // 6.0.0+
    class IScreenShotApplicationService : IpcService
    {
        public IScreenShotApplicationService(ServiceCtx context) { }

        [CommandHipc(32)] // 7.0.0+
        // SetShimLibraryVersion(pid, u64, nn::applet::AppletResourceUserId)
        public ResultCode SetShimLibraryVersion(ServiceCtx context)
        {
            return context.Device.System.CaptureManager.SetShimLibraryVersion(context);
        }

        [CommandHipc(203)]
        // SaveScreenShotEx0(bytes<0x40> ScreenShotAttribute, u32 unknown, u64 AppletResourceUserId, pid, buffer<bytes, 0x45> ScreenshotData) -> bytes<0x20> ApplicationAlbumEntry
        public ResultCode SaveScreenShotEx0(ServiceCtx context)
        {
            // TODO: Use the ScreenShotAttribute.
            ScreenShotAttribute screenShotAttribute = context.RequestData.ReadStruct<ScreenShotAttribute>();

            uint  unknown              = context.RequestData.ReadUInt32();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();
            ulong pidPlaceholder       = context.RequestData.ReadUInt64();

            ulong screenshotDataPosition = context.Request.SendBuff[0].Position;
            ulong screenshotDataSize     = context.Request.SendBuff[0].Size;

            byte[] screenshotData = context.Memory.GetSpan(screenshotDataPosition, (int)screenshotDataSize, true).ToArray();

            ResultCode resultCode = context.Device.System.CaptureManager.SaveScreenShot(screenshotData, appletResourceUserId, context.Device.Application.TitleId, out ApplicationAlbumEntry applicationAlbumEntry);

            context.ResponseData.WriteStruct(applicationAlbumEntry);

            return resultCode;
        }

        [CommandHipc(205)] // 8.0.0+
        // SaveScreenShotEx1(bytes<0x40> ScreenShotAttribute, u32 unknown, u64 AppletResourceUserId, pid, buffer<bytes, 0x15> ApplicationData, buffer<bytes, 0x45> ScreenshotData) -> bytes<0x20> ApplicationAlbumEntry
        public ResultCode SaveScreenShotEx1(ServiceCtx context)
        {
            // TODO: Use the ScreenShotAttribute.
            ScreenShotAttribute screenShotAttribute = context.RequestData.ReadStruct<ScreenShotAttribute>();

            uint  unknown              = context.RequestData.ReadUInt32();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();
            ulong pidPlaceholder       = context.RequestData.ReadUInt64();

            ulong applicationDataPosition = context.Request.SendBuff[0].Position;
            ulong applicationDataSize     = context.Request.SendBuff[0].Size;

            ulong screenshotDataPosition = context.Request.SendBuff[1].Position;
            ulong screenshotDataSize     = context.Request.SendBuff[1].Size;

            // TODO: Parse the application data: At 0x00 it's UserData (Size of 0x400), at 0x404 it's a uint UserDataSize (Always empty for now).
            byte[] applicationData = context.Memory.GetSpan(applicationDataPosition, (int)applicationDataSize).ToArray();

            byte[] screenshotData = context.Memory.GetSpan(screenshotDataPosition, (int)screenshotDataSize, true).ToArray();

            ResultCode resultCode = context.Device.System.CaptureManager.SaveScreenShot(screenshotData, appletResourceUserId, context.Device.Application.TitleId, out ApplicationAlbumEntry applicationAlbumEntry);

            context.ResponseData.WriteStruct(applicationAlbumEntry);

            return resultCode;
        }

        [CommandHipc(210)]
        // SaveScreenShotEx2(bytes<0x40> ScreenShotAttribute, u32 unknown, u64 AppletResourceUserId, buffer<bytes, 0x15> UserIdList, buffer<bytes, 0x45> ScreenshotData) -> bytes<0x20> ApplicationAlbumEntry
        public ResultCode SaveScreenShotEx2(ServiceCtx context)
        {
            // TODO: Use the ScreenShotAttribute.
            ScreenShotAttribute screenShotAttribute = context.RequestData.ReadStruct<ScreenShotAttribute>();

            uint  unknown              = context.RequestData.ReadUInt32();
            ulong appletResourceUserId = context.RequestData.ReadUInt64();

            ulong userIdListPosition = context.Request.SendBuff[0].Position;
            ulong userIdListSize     = context.Request.SendBuff[0].Size;

            ulong screenshotDataPosition = context.Request.SendBuff[1].Position;
            ulong screenshotDataSize     = context.Request.SendBuff[1].Size;

            // TODO: Parse the UserIdList.
            byte[] userIdList = context.Memory.GetSpan(userIdListPosition, (int)userIdListSize).ToArray();

            byte[] screenshotData = context.Memory.GetSpan(screenshotDataPosition, (int)screenshotDataSize, true).ToArray();

            ResultCode resultCode = context.Device.System.CaptureManager.SaveScreenShot(screenshotData, appletResourceUserId, context.Device.Application.TitleId, out ApplicationAlbumEntry applicationAlbumEntry);

            context.ResponseData.WriteStruct(applicationAlbumEntry);

            return resultCode;
        }
    }
}