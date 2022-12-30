﻿using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Account.Acc.AccountService;

namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    [Service("acc:su", AccountServiceFlag.Administrator)] // Max Sessions: 8
    class IAccountServiceForAdministrator : IpcService
    {
        private ApplicationServiceServer _applicationServiceServer;

        public IAccountServiceForAdministrator(ServiceCtx context, AccountServiceFlag serviceFlag)
        {
            _applicationServiceServer = new ApplicationServiceServer(serviceFlag);
        }

        [CommandHipc(0)]
        // GetUserCount() -> i32
        public ResultCode GetUserCount(ServiceCtx context)
        {
            return _applicationServiceServer.GetUserCountImpl(context);
        }

        [CommandHipc(1)]
        // GetUserExistence(nn::account::Uid) -> bool
        public ResultCode GetUserExistence(ServiceCtx context)
        {
            return _applicationServiceServer.GetUserExistenceImpl(context);
        }

        [CommandHipc(2)]
        // ListAllUsers() -> array<nn::account::Uid, 0xa>
        public ResultCode ListAllUsers(ServiceCtx context)
        {
            return _applicationServiceServer.ListAllUsers(context);
        }

        [CommandHipc(3)]
        // ListOpenUsers() -> array<nn::account::Uid, 0xa>
        public ResultCode ListOpenUsers(ServiceCtx context)
        {
            return _applicationServiceServer.ListOpenUsers(context);
        }

        [CommandHipc(4)]
        // GetLastOpenedUser() -> nn::account::Uid
        public ResultCode GetLastOpenedUser(ServiceCtx context)
        {
            return _applicationServiceServer.GetLastOpenedUser(context);
        }

        [CommandHipc(5)]
        // GetProfile(nn::account::Uid) -> object<nn::account::profile::IProfile>
        public ResultCode GetProfile(ServiceCtx context)
        {
            ResultCode resultCode = _applicationServiceServer.GetProfile(context, out IProfile iProfile);

            if (resultCode == ResultCode.Success)
            {
                MakeObject(context, iProfile);
            }

            return resultCode;
        }

        [CommandHipc(50)]
        // IsUserRegistrationRequestPermitted(pid) -> bool
        public ResultCode IsUserRegistrationRequestPermitted(ServiceCtx context)
        {
            // NOTE: pid is unused.

            return _applicationServiceServer.IsUserRegistrationRequestPermitted(context);
        }

        [CommandHipc(51)]
        // TrySelectUserWithoutInteraction(bool) -> nn::account::Uid
        public ResultCode TrySelectUserWithoutInteraction(ServiceCtx context)
        {
            return _applicationServiceServer.TrySelectUserWithoutInteraction(context);
        }

        [CommandHipc(102)]
        // GetBaasAccountManagerForSystemService(nn::account::Uid) -> object<nn::account::baas::IManagerForApplication>
        public ResultCode GetBaasAccountManagerForSystemService(ServiceCtx context)
        {
            ResultCode resultCode = _applicationServiceServer.CheckUserId(context, out UserId userId);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            MakeObject(context, new IManagerForSystemService(userId));

            // Doesn't occur in our case.
            // return ResultCode.NullObject;

            return ResultCode.Success;
        }

        [CommandHipc(140)] // 6.0.0+
        // ListQualifiedUsers() -> array<nn::account::Uid, 0xa>
        public ResultCode ListQualifiedUsers(ServiceCtx context)
        {
            return _applicationServiceServer.ListQualifiedUsers(context);
        }

        [CommandHipc(205)]
        // GetProfileEditor(nn::account::Uid) -> object<nn::account::profile::IProfileEditor>
        public ResultCode GetProfileEditor(ServiceCtx context)
        {
            UserId userId = context.RequestData.ReadStruct<UserId>();

            if (!context.Device.System.AccountManager.TryGetUser(userId, out UserProfile userProfile))
            {
                Logger.Warning?.Print(LogClass.ServiceAcc, $"User 0x{userId} not found!");

                return ResultCode.UserNotFound;
            }

            MakeObject(context, new IProfileEditor(userProfile));

            // Doesn't occur in our case.
            // return ResultCode.NullObject;

            return ResultCode.Success;
        }
    }
}