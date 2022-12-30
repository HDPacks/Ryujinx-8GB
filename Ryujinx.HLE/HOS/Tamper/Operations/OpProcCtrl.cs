namespace Ryujinx.HLE.HOS.Tamper.Operations
{
    class OpProcCtrl : IOperation
    {
        private ITamperedProcess _process;
        private bool _pause;

        public OpProcCtrl(ITamperedProcess process, bool pause)
        {
            _process = process;
            _pause = pause;
        }

        public void Execute()
        {
            if (_pause)
            {
                _process.PauseProcess();
            }
            else
            {
                _process.ResumeProcess();
            }
        }
    }
}
