using MsgPack;
using MsgPack.Serialization;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.Utilities;
using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Prepo
{
    [Service("prepo:a",  PrepoServicePermissionLevel.Admin)] // 1.0.0-5.1.0
    [Service("prepo:a2", PrepoServicePermissionLevel.Admin)] // 6.0.0+
    [Service("prepo:m",  PrepoServicePermissionLevel.Manager)]
    [Service("prepo:u",  PrepoServicePermissionLevel.User)]
    [Service("prepo:s",  PrepoServicePermissionLevel.System)]
    class IPrepoService : IpcService
    {
        private PrepoServicePermissionLevel _permission;
        private ulong _systemSessionId;

        public IPrepoService(ServiceCtx context, PrepoServicePermissionLevel permission)
        {
            _permission = permission;
        }

        [CommandHipc(10100)] // 1.0.0-5.1.0
        [CommandHipc(10102)] // 6.0.0-9.2.0
        [CommandHipc(10104)] // 10.0.0+
        // SaveReport(u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveReport(ServiceCtx context)
        {
            if ((_permission & PrepoServicePermissionLevel.User) == 0)
            {
                return ResultCode.PermissionDenied;
            }

            // We don't care about the differences since we don't use the play report.
            return ProcessReport(context, withUserID: false);
        }

        [CommandHipc(10101)] // 1.0.0-5.1.0
        [CommandHipc(10103)] // 6.0.0-9.2.0
        [CommandHipc(10105)] // 10.0.0+
        // SaveReportWithUser(nn::account::Uid, u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveReportWithUser(ServiceCtx context)
        {
            if ((_permission & PrepoServicePermissionLevel.User) == 0)
            {
                return ResultCode.PermissionDenied;
            }

            // We don't care about the differences since we don't use the play report.
            return ProcessReport(context, withUserID: true);
        }

        [CommandHipc(10200)]
        // RequestImmediateTransmission()
        public ResultCode RequestImmediateTransmission(ServiceCtx context)
        {
            // It signals an event of nn::prepo::detail::service::core::TransmissionStatusManager that requests the transmission of the report.
            // Since we don't use reports it's fine to do nothing.

            return ResultCode.Success;
        }

        [CommandHipc(10300)]
        // GetTransmissionStatus() -> u32
        public ResultCode GetTransmissionStatus(ServiceCtx context)
        {
            // It returns the transmission result of nn::prepo::detail::service::core::TransmissionStatusManager.
            // Since we don't use reports it's fine to return ResultCode.Success.
            context.ResponseData.Write((int)ResultCode.Success);

            return ResultCode.Success;
        }

        [CommandHipc(10400)] // 9.0.0+
        // GetSystemSessionId() -> u64
        public ResultCode GetSystemSessionId(ServiceCtx context)
        {
            if ((_permission & PrepoServicePermissionLevel.User) == 0)
            {
                return ResultCode.PermissionDenied;
            }

            if (_systemSessionId == 0)
            {
                byte[] randomBuffer = new byte[8];

                Random.Shared.NextBytes(randomBuffer);

                _systemSessionId = BitConverter.ToUInt64(randomBuffer, 0);
            }

            context.ResponseData.Write(_systemSessionId);

            return ResultCode.Success;
        }

        [CommandHipc(20100)]
        // SaveSystemReport(u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveSystemReport(ServiceCtx context)
        {
            if ((_permission & PrepoServicePermissionLevel.System) != 0)
            {
                return ResultCode.PermissionDenied;
            }

            // We don't care about the differences since we don't use the play report.
            return ProcessReport(context, withUserID: false);
        }

        [CommandHipc(20101)]
        // SaveSystemReportWithUser(nn::account::Uid, u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveSystemReportWithUser(ServiceCtx context)
        {
            if ((_permission & PrepoServicePermissionLevel.System) != 0)
            {
                return ResultCode.PermissionDenied;
            }

            // We don't care about the differences since we don't use the play report.
            return ProcessReport(context, withUserID: true);
        }

        private ResultCode ProcessReport(ServiceCtx context, bool withUserID)
        {
            UserId userId   = withUserID ? context.RequestData.ReadStruct<UserId>() : new UserId();
            string gameRoom = StringUtils.ReadUtf8String(context);

            if (withUserID)
            {
                if (userId.IsNull)
                {
                    return ResultCode.InvalidArgument;
                }
            }

            if (gameRoom == string.Empty)
            {
                return ResultCode.InvalidState;
            }

            ulong inputPosition = context.Request.SendBuff[0].Position;
            ulong inputSize     = context.Request.SendBuff[0].Size;

            if (inputSize == 0)
            {
                return ResultCode.InvalidBufferSize;
            }

            byte[] inputBuffer = new byte[inputSize];

            context.Memory.Read(inputPosition, inputBuffer);

            Logger.Info?.Print(LogClass.ServicePrepo, ReadReportBuffer(inputBuffer, gameRoom, userId));

            return ResultCode.Success;
        }

        private string ReadReportBuffer(byte[] buffer, string room, UserId userId)
        {
            StringBuilder     builder            = new StringBuilder();
            MessagePackObject deserializedReport = MessagePackSerializer.UnpackMessagePackObject(buffer);

            builder.AppendLine();
            builder.AppendLine("PlayReport log:");

            if (!userId.IsNull)
            {
                builder.AppendLine($" UserId: {userId}");
            }

            builder.AppendLine($" Room: {room}");
            builder.AppendLine($" Report: {MessagePackObjectFormatter.Format(deserializedReport)}");

            return builder.ToString();
        }
    }
}