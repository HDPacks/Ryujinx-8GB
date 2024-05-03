using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Loader;
using LibHac.Ncm;
using LibHac.Ns;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS;
using System.IO;
using System.Linq;
using ApplicationId = LibHac.Ncm.ApplicationId;

namespace Ryujinx.HLE.Loaders.Processes.Extensions
{
    static class NcaExtensions
    {
        public static ProcessResult Load(this Nca nca, Switch device, Nca patchNca, Nca controlNca)
        {
            // Extract RomFs and ExeFs from NCA.
            IStorage romFs = nca.GetRomFs(device, patchNca);
            IFileSystem exeFs = nca.GetExeFs(device, patchNca);

            if (exeFs == null)
            {
                Logger.Error?.Print(LogClass.Loader, "No ExeFS found in NCA");

                return ProcessResult.Failed;
            }

            // Load Npdm file.
            MetaLoader metaLoader = exeFs.GetNpdm();

            // Collecting mods related to AocTitleIds and ProgramId.
            device.Configuration.VirtualFileSystem.ModLoader.CollectMods(
                device.Configuration.ContentManager.GetAocTitleIds().Prepend(metaLoader.GetProgramId()),
                ModLoader.GetModsBasePath(),
                ModLoader.GetSdModsBasePath());

            // Load Nacp file.
            var nacpData = new BlitStruct<ApplicationControlProperty>(1);

            if (controlNca != null)
            {
                nacpData = controlNca.GetNacp(device);
            }

            /* TODO: Rework this since it's wrong and doesn't work as it takes the DisplayVersion from a "potential" inexistant update.

            // Load program 0 control NCA as we are going to need it for display version.
            (_, Nca updateProgram0ControlNca) = GetGameUpdateData(_device.Configuration.VirtualFileSystem, mainNca.Header.TitleId.ToString("x16"), 0, out _);

            // NOTE: Nintendo doesn't guarantee that the display version will be updated on sub programs when updating a multi program application.
            //       As such, to avoid PTC cache confusion, we only trust the program 0 display version when launching a sub program.
            if (updateProgram0ControlNca != null && _device.Configuration.UserChannelPersistence.Index != 0)
            {
                nacpData.Value.DisplayVersion = updateProgram0ControlNca.GetNacp(_device).Value.DisplayVersion;
            }

            */

            ProcessResult processResult = exeFs.Load(device, nacpData, metaLoader, (byte)nca.GetProgramIndex());

            // Load RomFS.
            if (romFs == null)
            {
                Logger.Warning?.Print(LogClass.Loader, "No RomFS found in NCA");
            }
            else
            {
                romFs = device.Configuration.VirtualFileSystem.ModLoader.ApplyRomFsMods(processResult.ProgramId, romFs);

                device.Configuration.VirtualFileSystem.SetRomFs(processResult.ProcessId, romFs.AsStream(FileAccess.Read));
            }

            // Don't create save data for system programs.
            if (processResult.ProgramId != 0 && (processResult.ProgramId < SystemProgramId.Start.Value || processResult.ProgramId > SystemAppletId.End.Value))
            {
                // Multi-program applications can technically use any program ID for the main program, but in practice they always use 0 in the low nibble.
                // We'll know if this changes in the future because applications will get errors when trying to mount the correct save.
                ProcessLoaderHelper.EnsureSaveData(device, new ApplicationId(processResult.ProgramId & ~0xFul), nacpData);
            }

            return processResult;
        }

        public static int GetProgramIndex(this Nca nca)
        {
            return (int)(nca.Header.TitleId & 0xF);
        }

        public static bool IsProgram(this Nca nca)
        {
            return nca.Header.ContentType == NcaContentType.Program;
        }

        public static bool IsPatch(this Nca nca)
        {
            int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

            return nca.IsProgram() && nca.SectionExists(NcaSectionType.Data) && nca.Header.GetFsHeader(dataIndex).IsPatchSection();
        }

        public static bool IsControl(this Nca nca)
        {
            return nca.Header.ContentType == NcaContentType.Control;
        }

        public static IFileSystem GetExeFs(this Nca nca, Switch device, Nca patchNca = null)
        {
            IFileSystem exeFs = null;

            if (patchNca == null)
            {
                if (nca.CanOpenSection(NcaSectionType.Code))
                {
                    exeFs = nca.OpenFileSystem(NcaSectionType.Code, device.System.FsIntegrityCheckLevel);
                }
            }
            else
            {
                if (patchNca.CanOpenSection(NcaSectionType.Code))
                {
                    exeFs = nca.OpenFileSystemWithPatch(patchNca, NcaSectionType.Code, device.System.FsIntegrityCheckLevel);
                }
            }

            return exeFs;
        }

        public static IStorage GetRomFs(this Nca nca, Switch device, Nca patchNca = null)
        {
            IStorage romFs = null;

            if (patchNca == null)
            {
                if (nca.CanOpenSection(NcaSectionType.Data))
                {
                    romFs = nca.OpenStorage(NcaSectionType.Data, device.System.FsIntegrityCheckLevel);
                }
            }
            else
            {
                if (patchNca.CanOpenSection(NcaSectionType.Data))
                {
                    romFs = nca.OpenStorageWithPatch(patchNca, NcaSectionType.Data, device.System.FsIntegrityCheckLevel);
                }
            }

            return romFs;
        }

        public static BlitStruct<ApplicationControlProperty> GetNacp(this Nca controlNca, Switch device)
        {
            var nacpData = new BlitStruct<ApplicationControlProperty>(1);

            using var controlFile = new UniqueRef<IFile>();

            Result result = controlNca.OpenFileSystem(NcaSectionType.Data, device.System.FsIntegrityCheckLevel)
                                      .OpenFile(ref controlFile.Ref, "/control.nacp".ToU8Span(), OpenMode.Read);

            if (result.IsSuccess())
            {
                result = controlFile.Get.Read(out long bytesRead, 0, nacpData.ByteSpan, ReadOption.None);
            }
            else
            {
                nacpData.ByteSpan.Clear();
            }

            return nacpData;
        }
    }
}
