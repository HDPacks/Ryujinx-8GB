﻿using System.Net.NetworkInformation;

namespace Ryujinx.Common.Utilities
{
    public static class NetworkHelpers
    {
        private static (IPInterfaceProperties, UnicastIPAddressInformation) GetLocalInterface(NetworkInterface adapter, bool isPreferred)
        {
            IPInterfaceProperties properties = adapter.GetIPProperties();

            if (isPreferred || (properties.GatewayAddresses.Count > 0 && properties.DnsAddresses.Count > 0))
            {
                foreach (UnicastIPAddressInformation info in properties.UnicastAddresses)
                {
                    // Only accept an IPv4 address
                    if (info.Address.GetAddressBytes().Length == 4)
                    {
                        return (properties, info);
                    }
                }
            }

            return (null, null);
        }

        public static (IPInterfaceProperties, UnicastIPAddressInformation) GetLocalInterface(string lanInterfaceId = "0")
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return (null, null);
            }

            IPInterfaceProperties       targetProperties  = null;
            UnicastIPAddressInformation targetAddressInfo = null;

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            string guid = lanInterfaceId;
            bool hasPreference = guid != "0";

            foreach (NetworkInterface adapter in interfaces)
            {
                bool isPreferred = adapter.Id == guid;

                // Ignore loopback and non IPv4 capable interface.
                if (isPreferred || (targetProperties == null && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && adapter.Supports(NetworkInterfaceComponent.IPv4)))
                {
                    (IPInterfaceProperties properties, UnicastIPAddressInformation info) = GetLocalInterface(adapter, isPreferred);

                    if (properties != null)
                    {
                        targetProperties = properties;
                        targetAddressInfo = info;

                        if (isPreferred || !hasPreference)
                        {
                            break;
                        }
                    }
                }
            }

            return (targetProperties, targetAddressInfo);
        }
    }
}