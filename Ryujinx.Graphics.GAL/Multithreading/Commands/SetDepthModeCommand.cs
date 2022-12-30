﻿namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetDepthModeCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetDepthMode;
        private DepthMode _mode;

        public void Set(DepthMode mode)
        {
            _mode = mode;
        }

        public static void Run(ref SetDepthModeCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetDepthMode(command._mode);
        }
    }
}
