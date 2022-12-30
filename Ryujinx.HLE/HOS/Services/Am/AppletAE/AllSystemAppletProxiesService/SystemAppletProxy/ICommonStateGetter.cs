using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Settings.Types;
using Ryujinx.HLE.HOS.Services.Vi.RootService.ApplicationDisplayService;
using Ryujinx.HLE.HOS.SystemState;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy
{
    class ICommonStateGetter : IpcService
    {
        private Apm.ManagerServer       _apmManagerServer;
        private Apm.SystemManagerServer _apmSystemManagerServer;
        private Lbl.LblControllerServer _lblControllerServer;

        private bool _vrModeEnabled;
#pragma warning disable CS0414
        private bool _lcdBacklighOffEnabled;
        private bool _requestExitToLibraryAppletAtExecuteNextProgramEnabled;
#pragma warning restore CS0414
        private int  _messageEventHandle;
        private int  _displayResolutionChangedEventHandle;

        public ICommonStateGetter(ServiceCtx context)
        {
            _apmManagerServer       = new Apm.ManagerServer(context);
            _apmSystemManagerServer = new Apm.SystemManagerServer(context);
            _lblControllerServer    = new Lbl.LblControllerServer(context);
        }

        [CommandHipc(0)]
        // GetEventHandle() -> handle<copy>
        public ResultCode GetEventHandle(ServiceCtx context)
        {
            KEvent messageEvent = context.Device.System.AppletState.MessageEvent;

            if (_messageEventHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(messageEvent.ReadableEvent, out _messageEventHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_messageEventHandle);

            return ResultCode.Success;
        }

        [CommandHipc(1)]
        // ReceiveMessage() -> nn::am::AppletMessage
        public ResultCode ReceiveMessage(ServiceCtx context)
        {
            if (!context.Device.System.AppletState.Messages.TryDequeue(out AppletMessage message))
            {
                return ResultCode.NoMessages;
            }

            KEvent messageEvent = context.Device.System.AppletState.MessageEvent;

            // NOTE: Service checks if current states are different than the stored ones.
            //       Since we don't support any states for now, it's fine to check if there is still messages available.

            if (context.Device.System.AppletState.Messages.IsEmpty)
            {
                messageEvent.ReadableEvent.Clear();
            }
            else
            {
                messageEvent.ReadableEvent.Signal();
            }

            context.ResponseData.Write((int)message);

            return ResultCode.Success;
        }

        [CommandHipc(5)]
        // GetOperationMode() -> u8
        public ResultCode GetOperationMode(ServiceCtx context)
        {
            OperationMode mode = context.Device.System.State.DockedMode
                ? OperationMode.Docked
                : OperationMode.Handheld;

            context.ResponseData.Write((byte)mode);

            return ResultCode.Success;
        }

        [CommandHipc(6)]
        // GetPerformanceMode() -> nn::apm::PerformanceMode
        public ResultCode GetPerformanceMode(ServiceCtx context)
        {
            return (ResultCode)_apmManagerServer.GetPerformanceMode(context);
        }

        [CommandHipc(8)]
        // GetBootMode() -> u8
        public ResultCode GetBootMode(ServiceCtx context)
        {
            context.ResponseData.Write((byte)0); //Unknown value.

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [CommandHipc(9)]
        // GetCurrentFocusState() -> u8
        public ResultCode GetCurrentFocusState(ServiceCtx context)
        {
            context.ResponseData.Write((byte)context.Device.System.AppletState.FocusState);

            return ResultCode.Success;
        }

        [CommandHipc(50)] // 3.0.0+
        // IsVrModeEnabled() -> b8
        public ResultCode IsVrModeEnabled(ServiceCtx context)
        {
            context.ResponseData.Write(_vrModeEnabled);

            return ResultCode.Success;
        }

        [CommandHipc(51)] // 3.0.0+
        // SetVrModeEnabled(b8)
        public ResultCode SetVrModeEnabled(ServiceCtx context)
        {
            bool vrModeEnabled = context.RequestData.ReadBoolean();

            UpdateVrMode(vrModeEnabled);

            return ResultCode.Success;
        }

        [CommandHipc(52)] // 4.0.0+
        // SetLcdBacklighOffEnabled(b8)
        public ResultCode SetLcdBacklighOffEnabled(ServiceCtx context)
        {
            // NOTE: Service sets a private field here, maybe this field is used somewhere else to turned off the backlight.
            //       Since we don't support backlight, it's fine to do nothing.

            _lcdBacklighOffEnabled = context.RequestData.ReadBoolean();

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [CommandHipc(53)] // 7.0.0+
        // BeginVrModeEx()
        public ResultCode BeginVrModeEx(ServiceCtx context)
        {
            UpdateVrMode(true);

            return ResultCode.Success;
        }

        [CommandHipc(54)] // 7.0.0+
        // EndVrModeEx()
        public ResultCode EndVrModeEx(ServiceCtx context)
        {
            UpdateVrMode(false);

            return ResultCode.Success;
        }

        private void UpdateVrMode(bool vrModeEnabled)
        {
            if (_vrModeEnabled == vrModeEnabled)
            {
                return;
            }

            _vrModeEnabled = vrModeEnabled;

            if (vrModeEnabled)
            {
                _lblControllerServer.EnableVrMode();
            }
            else
            {
                _lblControllerServer.DisableVrMode();
            }

            // TODO: It signals an internal event of ICommonStateGetter. We have to determine where this event is used.
        }

        [CommandHipc(60)] // 3.0.0+
        // GetDefaultDisplayResolution() -> (u32, u32)
        public ResultCode GetDefaultDisplayResolution(ServiceCtx context)
        {
            // NOTE: Original service calls IOperationModeManager::GetDefaultDisplayResolution of omm service.
            //       IOperationModeManager::GetDefaultDisplayResolution of omm service call IManagerDisplayService::GetDisplayResolution of vi service.
            (ulong width, ulong height) = AndroidSurfaceComposerClient.GetDisplayInfo(context);

            context.ResponseData.Write((uint)width);
            context.ResponseData.Write((uint)height);

            return ResultCode.Success;
        }

        [CommandHipc(61)] // 3.0.0+
        // GetDefaultDisplayResolutionChangeEvent() -> handle<copy>
        public ResultCode GetDefaultDisplayResolutionChangeEvent(ServiceCtx context)
        {
            // NOTE: Original service calls IOperationModeManager::GetDefaultDisplayResolutionChangeEvent of omm service.
            if (_displayResolutionChangedEventHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(context.Device.System.DisplayResolutionChangeEvent.ReadableEvent, out _displayResolutionChangedEventHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_displayResolutionChangedEventHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [CommandHipc(62)] // 4.0.0+
        // GetHdcpAuthenticationState() -> s32 state
        public ResultCode GetHdcpAuthenticationState(ServiceCtx context)
        {
            context.ResponseData.Write(0);

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [CommandHipc(66)] // 6.0.0+
        // SetCpuBoostMode(u32 cpu_boost_mode)
        public ResultCode SetCpuBoostMode(ServiceCtx context)
        {
            uint cpuBoostMode = context.RequestData.ReadUInt32();

            if (cpuBoostMode > 1)
            {
                return ResultCode.InvalidParameters;
            }

            _apmSystemManagerServer.SetCpuBoostMode((Apm.CpuBoostMode)cpuBoostMode);

            // TODO: It signals an internal event of ICommonStateGetter. We have to determine where this event is used.

            return ResultCode.Success;
        }

        [CommandHipc(91)] // 7.0.0+
        // GetCurrentPerformanceConfiguration() -> nn::apm::PerformanceConfiguration
        public ResultCode GetCurrentPerformanceConfiguration(ServiceCtx context)
        {
            return (ResultCode)_apmSystemManagerServer.GetCurrentPerformanceConfiguration(context);
        }

        [CommandHipc(300)] // 9.0.0+
        // GetSettingsPlatformRegion() -> u8
        public ResultCode GetSettingsPlatformRegion(ServiceCtx context)
        {
            PlatformRegion platformRegion = context.Device.System.State.DesiredRegionCode == (uint)RegionCode.China ? PlatformRegion.China : PlatformRegion.Global;

            // FIXME: Call set:sys GetPlatformRegion
            context.ResponseData.Write((byte)platformRegion);

            return ResultCode.Success;
        }

        [CommandHipc(900)] // 11.0.0+
        // SetRequestExitToLibraryAppletAtExecuteNextProgramEnabled()
        public ResultCode SetRequestExitToLibraryAppletAtExecuteNextProgramEnabled(ServiceCtx context)
        {
            // TODO : Find where the field is used.
            _requestExitToLibraryAppletAtExecuteNextProgramEnabled = true;

            return ResultCode.Success;
        }
    }
}