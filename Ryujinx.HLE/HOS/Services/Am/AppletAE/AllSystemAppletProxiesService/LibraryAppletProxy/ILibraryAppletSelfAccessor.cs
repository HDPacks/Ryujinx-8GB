﻿using Ryujinx.Common;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.LibraryAppletProxy
{
    class ILibraryAppletSelfAccessor : IpcService
    {
        private AppletStandalone _appletStandalone = new AppletStandalone();

        public ILibraryAppletSelfAccessor(ServiceCtx context)
        {
            if (context.Device.Application.TitleId == 0x0100000000001009)
            {
                // Create MiiEdit data.
                _appletStandalone = new AppletStandalone()
                {
                    AppletId          = AppletId.MiiEdit,
                    LibraryAppletMode = LibraryAppletMode.AllForeground
                };

                byte[] miiEditInputData = new byte[0x100];
                miiEditInputData[0] = 0x03; // Hardcoded unknown value.

                _appletStandalone.InputData.Enqueue(miiEditInputData);
            }
            else
            {
                throw new NotImplementedException($"{context.Device.Application.TitleId} applet is not implemented.");
            }
        }

        [CommandHipc(0)]
        // PopInData() -> object<nn::am::service::IStorage>
        public ResultCode PopInData(ServiceCtx context)
        {
            byte[] appletData = _appletStandalone.InputData.Dequeue();

            if (appletData.Length == 0)
            {
                return ResultCode.NotAvailable;
            }

            MakeObject(context, new IStorage(appletData));

            return ResultCode.Success;
        }

        [CommandHipc(11)]
        // GetLibraryAppletInfo() -> nn::am::service::LibraryAppletInfo
        public ResultCode GetLibraryAppletInfo(ServiceCtx context)
        {
            LibraryAppletInfo libraryAppletInfo = new LibraryAppletInfo()
            {
                AppletId          = _appletStandalone.AppletId,
                LibraryAppletMode = _appletStandalone.LibraryAppletMode
            };

            context.ResponseData.WriteStruct(libraryAppletInfo);

            return ResultCode.Success;
        }

        [CommandHipc(14)]
        // GetCallerAppletIdentityInfo() -> nn::am::service::AppletIdentityInfo
        public ResultCode GetCallerAppletIdentityInfo(ServiceCtx context)
        {
            AppletIdentifyInfo appletIdentifyInfo = new AppletIdentifyInfo()
            {
                AppletId = AppletId.QLaunch,
                TitleId  = 0x0100000000001000
            };

            context.ResponseData.WriteStruct(appletIdentifyInfo);

            return ResultCode.Success;
        }
    }
}