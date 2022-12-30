﻿using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Nfc.NfcManager
{
    class INfc : IpcService
    {
        private NfcPermissionLevel _permissionLevel;
        private State _state;

        public INfc(NfcPermissionLevel permissionLevel)
        {
            _permissionLevel = permissionLevel;
            _state = State.NonInitialized;
        }

        [CommandHipc(0)]
        [CommandHipc(400)] // 4.0.0+
        // Initialize(u64, u64, pid, buffer<unknown, 5>)
        public ResultCode Initialize(ServiceCtx context)
        {
            _state = State.Initialized;

            Logger.Stub?.PrintStub(LogClass.ServiceNfc, new { _permissionLevel });

            return ResultCode.Success;
        }

        [CommandHipc(1)]
        [CommandHipc(401)] // 4.0.0+
        // Finalize()
        public ResultCode Finalize(ServiceCtx context)
        {
            _state = State.NonInitialized;

            Logger.Stub?.PrintStub(LogClass.ServiceNfc, new { _permissionLevel });

            return ResultCode.Success;
        }

        [CommandHipc(2)]
        [CommandHipc(402)] // 4.0.0+
        // GetState() -> u32
        public ResultCode GetState(ServiceCtx context) 
        {
            context.ResponseData.Write((int)_state);

            return ResultCode.Success;
        }

        [CommandHipc(3)]
        [CommandHipc(403)] // 4.0.0+
        // IsNfcEnabled() -> b8
        public ResultCode IsNfcEnabled(ServiceCtx context)
        {
            // NOTE: Write false value here could make nfp service not called.
            context.ResponseData.Write(true);

            Logger.Stub?.PrintStub(LogClass.ServiceNfc, new { _permissionLevel });

            return ResultCode.Success;
        }
    }
}