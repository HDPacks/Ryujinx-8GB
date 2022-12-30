﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.Loaders.Npdm
{
    public class ServiceAccessControl
    {
        public IReadOnlyDictionary<string, bool> Services { get; private set; }

        public ServiceAccessControl(Stream stream, int offset, int size)
        {
            stream.Seek(offset, SeekOrigin.Begin);

            BinaryReader reader = new BinaryReader(stream);

            int bytesRead = 0;

            Dictionary<string, bool> services = new Dictionary<string, bool>();

            while (bytesRead != size)
            {
                byte controlByte = reader.ReadByte();

                if (controlByte == 0)
                {
                    break;
                }

                int  length          = (controlByte & 0x07) + 1;
                bool registerAllowed = (controlByte & 0x80) != 0;

                services[Encoding.ASCII.GetString(reader.ReadBytes(length))] = registerAllowed;

                bytesRead += length + 1;
            }

            Services = new ReadOnlyDictionary<string, bool>(services);
        }
    }
}
