﻿using System;
using System.Net;

namespace Flekosoft.Common.Network.Tcp.Internals
{
    class SocketAsyncNetworkExchangeDriver : AsyncNetworkExchangeDriver
    {
        public bool SendData(byte[] data)
        {
            return Write(data);
        }

        protected override void ProcessByteInternal(byte data)
        {
            OnNewByteEvent(new[] { data }, ExchangeInterface.LocalEndpoint, ExchangeInterface.LocalEndpoint);
        }

        public event EventHandler<NetworkDataEventArgs> NewByteEvent;
        private void OnNewByteEvent(byte[] data, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            NewByteEvent?.Invoke(this, new NetworkDataEventArgs(data, localEndPoint, remoteEndPoint));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NewByteEvent = null;
            }
            base.Dispose(disposing);
        }
    }
}
