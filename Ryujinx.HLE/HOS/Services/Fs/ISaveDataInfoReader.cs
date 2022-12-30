﻿using LibHac;
using LibHac.Common;
using LibHac.Sf;

namespace Ryujinx.HLE.HOS.Services.Fs
{
    class ISaveDataInfoReader : DisposableIpcService
    {
        private SharedRef<LibHac.FsSrv.Sf.ISaveDataInfoReader> _baseReader;

        public ISaveDataInfoReader(ref SharedRef<LibHac.FsSrv.Sf.ISaveDataInfoReader> baseReader)
        {
            _baseReader = SharedRef<LibHac.FsSrv.Sf.ISaveDataInfoReader>.CreateMove(ref baseReader);
        }

        [CommandHipc(0)]
        // ReadSaveDataInfo() -> (u64, buffer<unknown, 6>)
        public ResultCode ReadSaveDataInfo(ServiceCtx context)
        {
            ulong bufferAddress = context.Request.ReceiveBuff[0].Position;
            ulong bufferLen = context.Request.ReceiveBuff[0].Size;

            using (var region = context.Memory.GetWritableRegion(bufferAddress, (int)bufferLen, true))
            {
                Result result = _baseReader.Get.Read(out long readCount, new OutBuffer(region.Memory.Span));

                context.ResponseData.Write(readCount);

                return (ResultCode)result.Value;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _baseReader.Destroy();
            }
        }
    }
}
