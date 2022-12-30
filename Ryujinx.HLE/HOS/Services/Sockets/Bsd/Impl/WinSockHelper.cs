﻿using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Types;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Impl
{
    static class WinSockHelper
    {
        private static readonly Dictionary<WsaError, LinuxError> _errorMap = new()
        {
            // WSAEINTR
            {WsaError.WSAEINTR,           LinuxError.EINTR},
            // WSAEWOULDBLOCK
            {WsaError.WSAEWOULDBLOCK,     LinuxError.EWOULDBLOCK},
            // WSAEINPROGRESS
            {WsaError.WSAEINPROGRESS,     LinuxError.EINPROGRESS},
            // WSAEALREADY
            {WsaError.WSAEALREADY,        LinuxError.EALREADY},
            // WSAENOTSOCK
            {WsaError.WSAENOTSOCK,        LinuxError.ENOTSOCK},
            // WSAEDESTADDRREQ
            {WsaError.WSAEDESTADDRREQ,    LinuxError.EDESTADDRREQ},
            // WSAEMSGSIZE
            {WsaError.WSAEMSGSIZE,        LinuxError.EMSGSIZE},
            // WSAEPROTOTYPE
            {WsaError.WSAEPROTOTYPE,      LinuxError.EPROTOTYPE},
            // WSAENOPROTOOPT
            {WsaError.WSAENOPROTOOPT,     LinuxError.ENOPROTOOPT},
            // WSAEPROTONOSUPPORT
            {WsaError.WSAEPROTONOSUPPORT, LinuxError.EPROTONOSUPPORT},
            // WSAESOCKTNOSUPPORT
            {WsaError.WSAESOCKTNOSUPPORT, LinuxError.ESOCKTNOSUPPORT},
            // WSAEOPNOTSUPP
            {WsaError.WSAEOPNOTSUPP,      LinuxError.EOPNOTSUPP},
            // WSAEPFNOSUPPORT
            {WsaError.WSAEPFNOSUPPORT,    LinuxError.EPFNOSUPPORT},
            // WSAEAFNOSUPPORT
            {WsaError.WSAEAFNOSUPPORT,    LinuxError.EAFNOSUPPORT},
            // WSAEADDRINUSE
            {WsaError.WSAEADDRINUSE,      LinuxError.EADDRINUSE},
            // WSAEADDRNOTAVAIL
            {WsaError.WSAEADDRNOTAVAIL,   LinuxError.EADDRNOTAVAIL},
            // WSAENETDOWN
            {WsaError.WSAENETDOWN,        LinuxError.ENETDOWN},
            // WSAENETUNREACH
            {WsaError.WSAENETUNREACH,     LinuxError.ENETUNREACH},
            // WSAENETRESET
            {WsaError.WSAENETRESET,       LinuxError.ENETRESET},
            // WSAECONNABORTED
            {WsaError.WSAECONNABORTED,    LinuxError.ECONNABORTED},
            // WSAECONNRESET
            {WsaError.WSAECONNRESET,      LinuxError.ECONNRESET},
            // WSAENOBUFS
            {WsaError.WSAENOBUFS,         LinuxError.ENOBUFS},
            // WSAEISCONN
            {WsaError.WSAEISCONN,         LinuxError.EISCONN},
            // WSAENOTCONN
            {WsaError.WSAENOTCONN,        LinuxError.ENOTCONN},
            // WSAESHUTDOWN
            {WsaError.WSAESHUTDOWN,       LinuxError.ESHUTDOWN},
            // WSAETOOMANYREFS
            {WsaError.WSAETOOMANYREFS,    LinuxError.ETOOMANYREFS},
            // WSAETIMEDOUT
            {WsaError.WSAETIMEDOUT,       LinuxError.ETIMEDOUT},
            // WSAECONNREFUSED
            {WsaError.WSAECONNREFUSED,    LinuxError.ECONNREFUSED},
            // WSAELOOP
            {WsaError.WSAELOOP,           LinuxError.ELOOP},
            // WSAENAMETOOLONG
            {WsaError.WSAENAMETOOLONG,    LinuxError.ENAMETOOLONG},
            // WSAEHOSTDOWN
            {WsaError.WSAEHOSTDOWN,       LinuxError.EHOSTDOWN},
            // WSAEHOSTUNREACH
            {WsaError.WSAEHOSTUNREACH,    LinuxError.EHOSTUNREACH},
            // WSAENOTEMPTY
            {WsaError.WSAENOTEMPTY,       LinuxError.ENOTEMPTY},
            // WSAEUSERS
            {WsaError.WSAEUSERS,          LinuxError.EUSERS},
            // WSAEDQUOT
            {WsaError.WSAEDQUOT,          LinuxError.EDQUOT},
            // WSAESTALE
            {WsaError.WSAESTALE,          LinuxError.ESTALE},
            // WSAEREMOTE
            {WsaError.WSAEREMOTE,         LinuxError.EREMOTE},
            // WSAEINVAL
            {WsaError.WSAEINVAL,          LinuxError.EINVAL},
            // WSAEFAULT
            {WsaError.WSAEFAULT,          LinuxError.EFAULT},
            // NOERROR
            {0, 0}
        };

        private static readonly Dictionary<BsdSocketOption, SocketOptionName> _soSocketOptionMap = new()
        {
            { BsdSocketOption.SoDebug,       SocketOptionName.Debug },
            { BsdSocketOption.SoReuseAddr,   SocketOptionName.ReuseAddress },
            { BsdSocketOption.SoKeepAlive,   SocketOptionName.KeepAlive },
            { BsdSocketOption.SoDontRoute,   SocketOptionName.DontRoute },
            { BsdSocketOption.SoBroadcast,   SocketOptionName.Broadcast },
            { BsdSocketOption.SoUseLoopBack, SocketOptionName.UseLoopback },
            { BsdSocketOption.SoLinger,      SocketOptionName.Linger },
            { BsdSocketOption.SoOobInline,   SocketOptionName.OutOfBandInline },
            { BsdSocketOption.SoReusePort,   SocketOptionName.ReuseAddress },
            { BsdSocketOption.SoSndBuf,      SocketOptionName.SendBuffer },
            { BsdSocketOption.SoRcvBuf,      SocketOptionName.ReceiveBuffer },
            { BsdSocketOption.SoSndLoWat,    SocketOptionName.SendLowWater },
            { BsdSocketOption.SoRcvLoWat,    SocketOptionName.ReceiveLowWater },
            { BsdSocketOption.SoSndTimeo,    SocketOptionName.SendTimeout },
            { BsdSocketOption.SoRcvTimeo,    SocketOptionName.ReceiveTimeout },
            { BsdSocketOption.SoError,       SocketOptionName.Error },
            { BsdSocketOption.SoType,        SocketOptionName.Type }
        };

        private static readonly Dictionary<BsdSocketOption, SocketOptionName> _ipSocketOptionMap = new()
        {
            { BsdSocketOption.IpOptions,              SocketOptionName.IPOptions },
            { BsdSocketOption.IpHdrIncl,              SocketOptionName.HeaderIncluded },
            { BsdSocketOption.IpTtl,                  SocketOptionName.IpTimeToLive },
            { BsdSocketOption.IpMulticastIf,          SocketOptionName.MulticastInterface },
            { BsdSocketOption.IpMulticastTtl,         SocketOptionName.MulticastTimeToLive },
            { BsdSocketOption.IpMulticastLoop,        SocketOptionName.MulticastLoopback },
            { BsdSocketOption.IpAddMembership,        SocketOptionName.AddMembership },
            { BsdSocketOption.IpDropMembership,       SocketOptionName.DropMembership },
            { BsdSocketOption.IpDontFrag,             SocketOptionName.DontFragment },
            { BsdSocketOption.IpAddSourceMembership,  SocketOptionName.AddSourceMembership },
            { BsdSocketOption.IpDropSourceMembership, SocketOptionName.DropSourceMembership }
        };

        private static readonly Dictionary<BsdSocketOption, SocketOptionName> _tcpSocketOptionMap = new()
        {
            { BsdSocketOption.TcpNoDelay,   SocketOptionName.NoDelay },
            { BsdSocketOption.TcpKeepIdle,  SocketOptionName.TcpKeepAliveTime },
            { BsdSocketOption.TcpKeepIntvl, SocketOptionName.TcpKeepAliveInterval },
            { BsdSocketOption.TcpKeepCnt,   SocketOptionName.TcpKeepAliveRetryCount }
        };

        public static LinuxError ConvertError(WsaError errorCode)
        {
            if (!_errorMap.TryGetValue(errorCode, out LinuxError errno))
            {
                errno = (LinuxError)errorCode;
            }

            return errno;
        }

        public static bool TryConvertSocketOption(BsdSocketOption option, SocketOptionLevel level, out SocketOptionName name)
        {
            var table = level switch
            {
                SocketOptionLevel.Socket => _soSocketOptionMap,
                SocketOptionLevel.IP => _ipSocketOptionMap,
                SocketOptionLevel.Tcp => _tcpSocketOptionMap,
                _ => null
            };

            if (table == null)
            {
                name = default;
                return false;
            }

            return table.TryGetValue(option, out name);
        }
    }
}