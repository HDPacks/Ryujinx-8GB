using LibHac;
using LibHac.Common;
using LibHac.Fs;
using Path = LibHac.FsSrv.Sf.Path;

namespace Ryujinx.HLE.HOS.Services.Fs.FileSystemProxy
{
    class IFileSystem : DisposableIpcService
    {
        private SharedRef<LibHac.FsSrv.Sf.IFileSystem> _fileSystem;

        public IFileSystem(ref SharedRef<LibHac.FsSrv.Sf.IFileSystem> provider)
        {
            _fileSystem = SharedRef<LibHac.FsSrv.Sf.IFileSystem>.CreateMove(ref provider);
        }

        public SharedRef<LibHac.FsSrv.Sf.IFileSystem> GetBaseFileSystem()
        {
            return SharedRef<LibHac.FsSrv.Sf.IFileSystem>.CreateCopy(in _fileSystem);
        }

        [CommandHipc(0)]
        // CreateFile(u32 createOption, u64 size, buffer<bytes<0x301>, 0x19, 0x301> path)
        public ResultCode CreateFile(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            int createOption = context.RequestData.ReadInt32();
            context.RequestData.BaseStream.Position += 4;

            long size = context.RequestData.ReadInt64();

            return (ResultCode)_fileSystem.Get.CreateFile(in name, size, createOption).Value;
        }

        [CommandHipc(1)]
        // DeleteFile(buffer<bytes<0x301>, 0x19, 0x301> path)
        public ResultCode DeleteFile(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            return (ResultCode)_fileSystem.Get.DeleteFile(in name).Value;
        }

        [CommandHipc(2)]
        // CreateDirectory(buffer<bytes<0x301>, 0x19, 0x301> path)
        public ResultCode CreateDirectory(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            return (ResultCode)_fileSystem.Get.CreateDirectory(in name).Value;
        }

        [CommandHipc(3)]
        // DeleteDirectory(buffer<bytes<0x301>, 0x19, 0x301> path)
        public ResultCode DeleteDirectory(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            return (ResultCode)_fileSystem.Get.DeleteDirectory(in name).Value;
        }

        [CommandHipc(4)]
        // DeleteDirectoryRecursively(buffer<bytes<0x301>, 0x19, 0x301> path)
        public ResultCode DeleteDirectoryRecursively(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            return (ResultCode)_fileSystem.Get.DeleteDirectoryRecursively(in name).Value;
        }

        [CommandHipc(5)]
        // RenameFile(buffer<bytes<0x301>, 0x19, 0x301> oldPath, buffer<bytes<0x301>, 0x19, 0x301> newPath)
        public ResultCode RenameFile(ServiceCtx context)
        {
            ref readonly Path currentName = ref FileSystemProxyHelper.GetSfPath(context, index: 0);
            ref readonly Path newName = ref FileSystemProxyHelper.GetSfPath(context, index: 1);

            return (ResultCode)_fileSystem.Get.RenameFile(in currentName, in newName).Value;
        }

        [CommandHipc(6)]
        // RenameDirectory(buffer<bytes<0x301>, 0x19, 0x301> oldPath, buffer<bytes<0x301>, 0x19, 0x301> newPath)
        public ResultCode RenameDirectory(ServiceCtx context)
        {
            ref readonly Path currentName = ref FileSystemProxyHelper.GetSfPath(context, index: 0);
            ref readonly Path newName = ref FileSystemProxyHelper.GetSfPath(context, index: 1);

            return (ResultCode)_fileSystem.Get.RenameDirectory(in currentName, in newName).Value;
        }

        [CommandHipc(7)]
        // GetEntryType(buffer<bytes<0x301>, 0x19, 0x301> path) -> nn::fssrv::sf::DirectoryEntryType
        public ResultCode GetEntryType(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            Result result = _fileSystem.Get.GetEntryType(out uint entryType, in name);

            context.ResponseData.Write((int)entryType);

            return (ResultCode)result.Value;
        }

        [CommandHipc(8)]
        // OpenFile(u32 mode, buffer<bytes<0x301>, 0x19, 0x301> path) -> object<nn::fssrv::sf::IFile> file
        public ResultCode OpenFile(ServiceCtx context)
        {
            uint mode = context.RequestData.ReadUInt32();

            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);
            using var file = new SharedRef<LibHac.FsSrv.Sf.IFile>();

            Result result = _fileSystem.Get.OpenFile(ref file.Ref(), in name, mode);

            if (result.IsSuccess())
            {
                IFile fileInterface = new IFile(ref file.Ref());

                MakeObject(context, fileInterface);
            }

            return (ResultCode)result.Value;
        }

        [CommandHipc(9)]
        // OpenDirectory(u32 filter_flags, buffer<bytes<0x301>, 0x19, 0x301> path) -> object<nn::fssrv::sf::IDirectory> directory
        public ResultCode OpenDirectory(ServiceCtx context)
        {
            uint mode = context.RequestData.ReadUInt32();

            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);
            using var dir = new SharedRef<LibHac.FsSrv.Sf.IDirectory>();

            Result result = _fileSystem.Get.OpenDirectory(ref dir.Ref(), name, mode);

            if (result.IsSuccess())
            {
                IDirectory dirInterface = new IDirectory(ref dir.Ref());

                MakeObject(context, dirInterface);
            }

            return (ResultCode)result.Value;
        }

        [CommandHipc(10)]
        // Commit()
        public ResultCode Commit(ServiceCtx context)
        {
            return (ResultCode)_fileSystem.Get.Commit().Value;
        }

        [CommandHipc(11)]
        // GetFreeSpaceSize(buffer<bytes<0x301>, 0x19, 0x301> path) -> u64 totalFreeSpace
        public ResultCode GetFreeSpaceSize(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            Result result = _fileSystem.Get.GetFreeSpaceSize(out long size, in name);

            context.ResponseData.Write(size);

            return (ResultCode)result.Value;
        }

        [CommandHipc(12)]
        // GetTotalSpaceSize(buffer<bytes<0x301>, 0x19, 0x301> path) -> u64 totalSize
        public ResultCode GetTotalSpaceSize(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            Result result = _fileSystem.Get.GetTotalSpaceSize(out long size, in name);

            context.ResponseData.Write(size);

            return (ResultCode)result.Value;
        }

        [CommandHipc(13)]
        // CleanDirectoryRecursively(buffer<bytes<0x301>, 0x19, 0x301> path)
        public ResultCode CleanDirectoryRecursively(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            return (ResultCode)_fileSystem.Get.CleanDirectoryRecursively(in name).Value;
        }

        [CommandHipc(14)]
        // GetFileTimeStampRaw(buffer<bytes<0x301>, 0x19, 0x301> path) -> bytes<0x20> timestamp
        public ResultCode GetFileTimeStampRaw(ServiceCtx context)
        {
            ref readonly Path name = ref FileSystemProxyHelper.GetSfPath(context);

            Result result = _fileSystem.Get.GetFileTimeStampRaw(out FileTimeStampRaw timestamp, in name);

            context.ResponseData.Write(timestamp.Created);
            context.ResponseData.Write(timestamp.Modified);
            context.ResponseData.Write(timestamp.Accessed);
            context.ResponseData.Write(1L); // Is valid?

            return (ResultCode)result.Value;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _fileSystem.Destroy();
            }
        }
    }
}