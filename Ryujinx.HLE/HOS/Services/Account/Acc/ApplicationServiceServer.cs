﻿using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Account.Acc.AccountService;
using Ryujinx.HLE.HOS.Services.Account.Acc.AsyncContext;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    class ApplicationServiceServer
    {
        readonly AccountServiceFlag _serviceFlag;

        public ApplicationServiceServer(AccountServiceFlag serviceFlag)
        {
            _serviceFlag = serviceFlag;
        }

        public ResultCode GetUserCountImpl(ServiceCtx context)
        {
            context.ResponseData.Write(context.Device.System.AccountManager.GetUserCount());

            return ResultCode.Success;
        }

        public ResultCode GetUserExistenceImpl(ServiceCtx context)
        {
            ResultCode resultCode = CheckUserId(context, out UserId userId);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            context.ResponseData.Write(context.Device.System.AccountManager.TryGetUser(userId, out _));

            return ResultCode.Success;
        }

        public ResultCode ListAllUsers(ServiceCtx context)
        {
            return WriteUserList(context, context.Device.System.AccountManager.GetAllUsers());
        }

        public ResultCode ListOpenUsers(ServiceCtx context)
        {
            return WriteUserList(context, context.Device.System.AccountManager.GetOpenedUsers());
        }

        private ResultCode WriteUserList(ServiceCtx context, IEnumerable<UserProfile> profiles)
        {
            if (context.Request.RecvListBuff.Count == 0)
            {
                return ResultCode.InvalidBuffer;
            }

            ulong outputPosition = context.Request.RecvListBuff[0].Position;
            ulong outputSize     = context.Request.RecvListBuff[0].Size;

            MemoryHelper.FillWithZeros(context.Memory, outputPosition, (int)outputSize);

            ulong offset = 0;

            foreach (UserProfile userProfile in profiles)
            {
                if (offset + 0x10 > outputSize)
                {
                    break;
                }

                context.Memory.Write(outputPosition + offset,     userProfile.UserId.High);
                context.Memory.Write(outputPosition + offset + 8, userProfile.UserId.Low);

                offset += 0x10;
            }

            return ResultCode.Success;
        }

        public ResultCode GetLastOpenedUser(ServiceCtx context)
        {
            context.Device.System.AccountManager.LastOpenedUser.UserId.Write(context.ResponseData);

            return ResultCode.Success;
        }

        public ResultCode GetProfile(ServiceCtx context, out IProfile profile)
        {
            profile = default;

            ResultCode resultCode = CheckUserId(context, out UserId userId);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            if (!context.Device.System.AccountManager.TryGetUser(userId, out UserProfile userProfile))
            {
                Logger.Warning?.Print(LogClass.ServiceAcc, $"User 0x{userId} not found!");

                return ResultCode.UserNotFound;
            }

            profile = new IProfile(userProfile);

            // Doesn't occur in our case.
            // return ResultCode.NullObject;

            return ResultCode.Success;
        }

        public ResultCode IsUserRegistrationRequestPermitted(ServiceCtx context)
        {
            context.ResponseData.Write(_serviceFlag != AccountServiceFlag.Application);

            return ResultCode.Success;
        }

        public ResultCode TrySelectUserWithoutInteraction(ServiceCtx context)
        {
            if (context.Device.System.AccountManager.GetUserCount() < 1)
            {
                // Invalid UserId.
                UserId.Null.Write(context.ResponseData);

                return ResultCode.UserNotFound;
            }

            bool isNetworkServiceAccountRequired = context.RequestData.ReadBoolean();

            if (isNetworkServiceAccountRequired)
            {
                // NOTE: This checks something related to baas (online), and then return an invalid UserId if the check in baas returns an error code.
                //       In our case, we can just log it for now.

                Logger.Stub?.PrintStub(LogClass.ServiceAcc, new { isNetworkServiceAccountRequired });
            }

            // NOTE: As we returned an invalid UserId if there is more than one user earlier, now we can return only the first one.
            context.Device.System.AccountManager.GetFirst().UserId.Write(context.ResponseData);

            return ResultCode.Success;
        }

        public ResultCode CheckNetworkServiceAvailabilityAsync(ServiceCtx context, out IAsyncContext asyncContext)
        {
            KEvent         asyncEvent     = new(context.Device.System.KernelContext);
            AsyncExecution asyncExecution = new(asyncEvent);

            asyncExecution.Initialize(1000, CheckNetworkServiceAvailabilityAsyncImpl);

            asyncContext = new IAsyncContext(asyncExecution);

            // return ResultCode.NullObject if the IAsyncContext pointer is null. Doesn't occur in our case.

            return ResultCode.Success;
        }

        private async Task CheckNetworkServiceAvailabilityAsyncImpl(CancellationToken token)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            // TODO: Use a real function instead, with the CancellationToken.
            await Task.CompletedTask;
        }

        public ResultCode StoreSaveDataThumbnail(ServiceCtx context)
        {
            ResultCode resultCode = CheckUserId(context, out UserId _);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            if (context.Request.SendBuff.Count == 0)
            {
                return ResultCode.InvalidBuffer;
            }

            ulong inputPosition = context.Request.SendBuff[0].Position;
            ulong inputSize     = context.Request.SendBuff[0].Size;

            if (inputSize != 0x24000)
            {
                return ResultCode.InvalidBufferSize;
            }

            byte[] thumbnailBuffer = new byte[inputSize];

            context.Memory.Read(inputPosition, thumbnailBuffer);

            // NOTE: Account service call nn::fs::WriteSaveDataThumbnailFile().
            // TODO: Store thumbnailBuffer somewhere, in save data 0x8000000000000010 ?

            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            return ResultCode.Success;
        }

        public ResultCode ClearSaveDataThumbnail(ServiceCtx context)
        {
            ResultCode resultCode = CheckUserId(context, out UserId _);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            /*
            // NOTE: Doesn't occur in our case.
            if (userId == null)
            {
                return ResultCode.InvalidArgument;
            }
            */

            // NOTE: Account service call nn::fs::WriteSaveDataThumbnailFileHeader();
            // TODO: Clear the Thumbnail somewhere, in save data 0x8000000000000010 ?

            Logger.Stub?.PrintStub(LogClass.ServiceAcc);

            return ResultCode.Success;
        }

        public ResultCode ListOpenContextStoredUsers(ServiceCtx context)
        {
            return WriteUserList(context, context.Device.System.AccountManager.GetStoredOpenedUsers());
        }

        public ResultCode ListQualifiedUsers(ServiceCtx context)
        {
            // TODO: Determine how users are "qualified". We assume all users are "qualified" for now.

            return WriteUserList(context, context.Device.System.AccountManager.GetAllUsers());
        }

        public ResultCode CheckUserId(ServiceCtx context, out UserId userId)
        {
            userId = context.RequestData.ReadStruct<UserId>();

            if (userId.IsNull)
            {
                return ResultCode.NullArgument;
            }

            return ResultCode.Success;
        }
    }
}