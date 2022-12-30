using Ryujinx.HLE.HOS.Tamper.Operations;

namespace Ryujinx.HLE.HOS.Tamper.Conditions
{
    class CondLT<T> : ICondition where T : unmanaged
    {
        private IOperand _lhs;
        private IOperand _rhs;

        public CondLT(IOperand lhs, IOperand rhs)
        {
            _lhs = lhs;
            _rhs = rhs;
        }

        public bool Evaluate()
        {
            return (dynamic)_lhs.Get<T>() < (dynamic)_rhs.Get<T>();
        }
    }
}
