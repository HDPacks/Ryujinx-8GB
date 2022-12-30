﻿using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.HOS.Applets.Browser
{
    internal class BrowserApplet : IApplet
    {
        public event EventHandler AppletStateChanged;

        private AppletSession _normalSession;
        private AppletSession _interactiveSession;

        private CommonArguments _commonArguments;
        private List<BrowserArgument> _arguments;
        private ShimKind _shimKind;

        public BrowserApplet(Horizon system) {}

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        public ResultCode Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            _normalSession = normalSession;
            _interactiveSession = interactiveSession;

            _commonArguments = IApplet.ReadStruct<CommonArguments>(_normalSession.Pop());

            Logger.Stub?.PrintStub(LogClass.ServiceAm, $"WebApplet version: 0x{_commonArguments.AppletVersion:x8}");

            ReadOnlySpan<byte> webArguments = _normalSession.Pop();

            (_shimKind, _arguments) = BrowserArgument.ParseArguments(webArguments);

            Logger.Stub?.PrintStub(LogClass.ServiceAm, $"Web Arguments: {_arguments.Count}");

            foreach (BrowserArgument argument in _arguments)
            {
                Logger.Stub?.PrintStub(LogClass.ServiceAm, $"{argument.Type}: {argument.GetValue()}");
            }

            if ((_commonArguments.AppletVersion >= 0x80000 && _shimKind == ShimKind.Web) || (_commonArguments.AppletVersion >= 0x30000 && _shimKind == ShimKind.Share))
            {
                List<BrowserOutput> result = new List<BrowserOutput>();

                result.Add(new BrowserOutput(BrowserOutputType.ExitReason, (uint)WebExitReason.ExitButton));

                _normalSession.Push(BuildResponseNew(result));
            }
            else
            {
                WebCommonReturnValue result = new WebCommonReturnValue()
                {
                    ExitReason  = WebExitReason.ExitButton,
                };

                _normalSession.Push(BuildResponseOld(result));
            }

            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        private byte[] BuildResponseOld(WebCommonReturnValue result)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.WriteStruct(result);

                return stream.ToArray();
            }
        }
        private byte[] BuildResponseNew(List<BrowserOutput> outputArguments)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.WriteStruct(new WebArgHeader
                {
                    Count    = (ushort)outputArguments.Count,
                    ShimKind = _shimKind
                });

                foreach (BrowserOutput output in outputArguments)
                {
                    output.Write(writer);
                }

                writer.Write(new byte[0x2000 - writer.BaseStream.Position]);

                return stream.ToArray();
            }
        }
    }
}
