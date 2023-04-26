using Ryujinx.Audio.Backends.CompatLayer;
using Ryujinx.Audio.Integration;
using Ryujinx.Common.Configuration;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Apm;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.Loaders.Processes;
using Ryujinx.HLE.Ui;
using Ryujinx.Memory;
using System;

namespace Ryujinx.HLE
{
    public class Switch : IDisposable
    {
        public HLEConfiguration      Configuration     { get; }
        public IHardwareDeviceDriver AudioDeviceDriver { get; }
        public MemoryBlock           Memory            { get; }
        public GpuContext            Gpu               { get; }
        public VirtualFileSystem     FileSystem        { get; }
        public HOS.Horizon           System            { get; }
        public ProcessLoader         Processes         { get; }
        public PerformanceStatistics Statistics        { get; }
        public Hid                   Hid               { get; }
        public TamperMachine         TamperMachine     { get; }
        public IHostUiHandler        UiHandler         { get; }

        public bool EnableDeviceVsync { get; set; } = true;

        public bool IsFrameAvailable => Gpu.Window.IsFrameAvailable;

        public Switch(HLEConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration.GpuRenderer);
            ArgumentNullException.ThrowIfNull(configuration.AudioDeviceDriver);
            ArgumentNullException.ThrowIfNull(configuration.UserChannelPersistence);

            Configuration = configuration;
            FileSystem    = Configuration.VirtualFileSystem;
            UiHandler     = Configuration.HostUiHandler;

            MemoryAllocationFlags memoryAllocationFlags = configuration.MemoryManagerMode == MemoryManagerMode.SoftwarePageTable
                ? MemoryAllocationFlags.Reserve
                : MemoryAllocationFlags.Reserve | MemoryAllocationFlags.Mirrorable;

            AudioDeviceDriver = new CompatLayerHardwareDeviceDriver(Configuration.AudioDeviceDriver);
            Memory            = new MemoryBlock(Configuration.MemoryConfiguration.ToDramSize(), memoryAllocationFlags);
            Gpu               = new GpuContext(Configuration.GpuRenderer);
            System            = new HOS.Horizon(this);
            Statistics        = new PerformanceStatistics();
            Hid               = new Hid(this, System.HidStorage);
            Processes         = new ProcessLoader(this);
            TamperMachine     = new TamperMachine();

            System.State.SetLanguage(Configuration.SystemLanguage);
            System.State.SetRegion(Configuration.Region);

            EnableDeviceVsync                       = Configuration.EnableVsync;
            System.State.DockedMode                 = Configuration.EnableDockedMode;
            System.PerformanceState.PerformanceMode = System.State.DockedMode ? PerformanceMode.Boost : PerformanceMode.Default;
            System.EnablePtc                        = Configuration.EnablePtc;
            System.FsIntegrityCheckLevel            = Configuration.FsIntegrityCheckLevel;
            System.GlobalAccessLogMode              = Configuration.FsGlobalAccessLogMode;
        }

        public bool LoadCart(string exeFsDir, string romFsFile = null)
        {
            return Processes.LoadUnpackedNca(exeFsDir, romFsFile);
        }

        public bool LoadXci(string xciFile)
        {
            return Processes.LoadXci(xciFile);
        }

        public bool LoadNca(string ncaFile)
        {
            return Processes.LoadNca(ncaFile);
        }

        public bool LoadNsp(string nspFile)
        {
            return Processes.LoadNsp(nspFile);
        }

        public bool LoadProgram(string fileName)
        {
            return Processes.LoadNxo(fileName);
        }

        public bool WaitFifo()
        {
            return Gpu.GPFifo.WaitForCommands();
        }

        public void ProcessFrame()
        {
            Gpu.ProcessShaderCacheQueue();
            Gpu.Renderer.PreFrame();
            Gpu.GPFifo.DispatchCalls();
        }

        public bool ConsumeFrameAvailable()
        {
            return Gpu.Window.ConsumeFrameAvailable();
        }

        public void PresentFrame(Action swapBuffersCallback)
        {
            Gpu.Window.Present(swapBuffersCallback);
        }

        public void SetVolume(float volume)
        {
            System.SetVolume(Math.Clamp(volume, 0, 1));
        }

        public float GetVolume()
        {
            return System.GetVolume();
        }

        public void EnableCheats()
        {
            FileSystem.ModLoader.EnableCheats(Processes.ActiveApplication.ProgramId, TamperMachine);
        }

        public bool IsAudioMuted()
        {
            return System.GetVolume() == 0;
        }

        public void DisposeGpu()
        {
            Gpu.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                System.Dispose();
                AudioDeviceDriver.Dispose();
                FileSystem.Dispose();
                Memory.Dispose();
            }
        }
    }
}
