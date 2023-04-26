using Ryujinx.Common.Utilities;
using Ryujinx.Horizon.Sdk.Sf.Cmif;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    struct Header
    {
        private uint _word0;
        private uint _word1;

        public CommandType Type
        {
            get => (CommandType)_word0.Extract(0, 16);
            set => _word0 = _word0.Insert(0, 16, (uint)value);
        }

        public int SendStaticsCount
        {
            get => (int)_word0.Extract(16, 4);
            set => _word0 = _word0.Insert(16, 4, (uint)value);
        }

        public int SendBuffersCount
        {
            get => (int)_word0.Extract(20, 4);
            set => _word0 = _word0.Insert(20, 4, (uint)value);
        }

        public int ReceiveBuffersCount
        {
            get => (int)_word0.Extract(24, 4);
            set => _word0 = _word0.Insert(24, 4, (uint)value);
        }

        public int ExchangeBuffersCount
        {
            get => (int)_word0.Extract(28, 4);
            set => _word0 = _word0.Insert(28, 4, (uint)value);
        }

        public int DataWordsCount
        {
            get => (int)_word1.Extract(0, 10);
            set => _word1 = _word1.Insert(0, 10, (uint)value);
        }

        public int ReceiveStaticMode
        {
            get => (int)_word1.Extract(10, 4);
            set => _word1 = _word1.Insert(10, 4, (uint)value);
        }

        public int ReceiveListOffset
        {
            get => (int)_word1.Extract(20, 11);
            set => _word1 = _word1.Insert(20, 11, (uint)value);
        }

        public bool HasSpecialHeader
        {
            get => _word1.Extract(31);
            set => _word1 = _word1.Insert(31, value);
        }
    }
}
