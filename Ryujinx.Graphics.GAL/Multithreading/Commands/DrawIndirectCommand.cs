namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct DrawIndirectCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.DrawIndirect;
        private BufferRange _indirectBuffer;

        public void Set(BufferRange indirectBuffer)
        {
            _indirectBuffer = indirectBuffer;
        }

        public static void Run(ref DrawIndirectCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.DrawIndirect(threaded.Buffers.MapBufferRange(command._indirectBuffer));
        }
    }
}
