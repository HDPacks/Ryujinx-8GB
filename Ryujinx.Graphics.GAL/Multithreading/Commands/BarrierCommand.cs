﻿namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct BarrierCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.Barrier;

        public static void Run(ref BarrierCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.Barrier();
        }
    }
}
