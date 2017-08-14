﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Flekosoft.Common.Network.Tcp
{
    public abstract class TcpClient : PropertyChangedErrorNotifyDisposableBase
    {
        private bool _isStarted;
        private bool _isConnected;

        private System.Net.Sockets.TcpClient _client;
        NetworkStream _netStream;

        private int _pingFailCount;

        private readonly Thread _connectThread;
        private readonly Thread _readFromStreamThread;
        private readonly Thread _processDataThread;

        private readonly ConcurrentQueue<byte> _bytesQueue = new ConcurrentQueue<byte>();
        readonly EventWaitHandle _hasDataWh = new EventWaitHandle(false, EventResetMode.ManualReset);

        private byte[] _readBuffer;
        private readonly object _readBufferSyncObject = new object();

        private int _pollFailLimit;
        private int _pollInterval;
        private int _readBufferSize;
        private int _connectInterval;

        protected TcpClient()
        {
            PollFailLimit = 3;
            PollInterval = 1000;
            ConnectInterval = 1000;
            ReadBufferSize = 1024;

            _connectThread = new Thread(ConnectThreadFunc);
            _connectThread.Start();
            _readFromStreamThread = new Thread(ReadFromStreamThreadFunc);
            _readFromStreamThread.Start();
            _processDataThread = new Thread(ProcessDataThreadFunc);
            _processDataThread.Start();
        }

        #region Properties

        /// <summary>
        /// Server Ip address
        /// </summary>
        public string IpAddress { get; protected set; }
        /// <summary>
        /// Server port
        /// </summary>
        public int Port { get; protected set; }
        /// <summary>
        /// Is connected to server
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }
        /// <summary>
        /// Is client started
        /// </summary>
        public bool IsStarted
        {
            get { return _isStarted; }
            private set
            {
                if (_isStarted != value)
                {
                    _isStarted = value;
                    OnPropertyChanged(nameof(IsStarted));
                    if (_isStarted) OnStartedEvent();
                    else OnStoppedEvent();
                }
            }
        }
        /// <summary>
        /// Count of failed polls to disconnect from server
        /// </summary>
        public int PollFailLimit
        {
            get { return _pollFailLimit; }
            set
            {
                if (_pollFailLimit != value)
                {
                    _pollFailLimit = value;
                    OnPropertyChanged(nameof(PollFailLimit));
                }
            }
        }
        /// <summary>
        /// Poll interval in milliseconds
        /// </summary>
        public int PollInterval
        {
            get { return _pollInterval; }
            set
            {
                if (_pollInterval != value)
                {
                    _pollInterval = value;
                    OnPropertyChanged(nameof(PollInterval));
                }
            }
        }
        /// <summary>
        /// Reconnect Interval in milliseconds
        /// </summary>
        public int ConnectInterval
        {
            get { return _connectInterval; }
            set
            {
                if (_connectInterval != value)
                {
                    _connectInterval = value;
                    OnPropertyChanged(nameof(ConnectInterval));
                }
            }
        }
        /// <summary>
        /// Socket read buffer size
        /// </summary>
        public int ReadBufferSize
        {
            get { return _readBufferSize; }
            set
            {
                if (_readBufferSize != value)
                {
                    _readBufferSize = value;
                    lock (_readBufferSyncObject)
                    {
                        _readBuffer = new byte[_readBufferSize];
                    }
                    OnPropertyChanged(nameof(ReadBufferSize));
                }
            }
        }

        #endregion

        #region Threads

        private void ConnectThreadFunc()
        {
            while (true)
            {
                try
                {
                    if (!IsStarted)
                    {
                        if (IsConnected)
                        {
                            DisconnectFromServer();
                        }
                        continue;
                    }

                    if (!IsConnected)
                    {
                        try
                        {
                            if (!IsDisposed)
                            {
                                OnReconnectingEvent();
                                if (ConnectToServer())
                                {
                                    _pingFailCount = 0;
                                }
                                else Thread.Sleep(ConnectInterval);
                            }
                        }
                        // ReSharper disable UnusedVariable
                        catch (Exception ex)
                        // ReSharper restore UnusedVariable
                        {
                            OnErrorEvent(ex);
                        }
                    }
                    else
                    {
                        Thread.Sleep(PollInterval);
                        //Poll
                        if (!Ping.Send(IpAddress))
                        {
                            _pingFailCount++;
                            if (_pingFailCount >= PollFailLimit)
                                DisconnectFromServer();
                        }
                        else _pingFailCount = 0;
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnErrorEvent(ex);
                }
            }
        }

        private void ReadFromStreamThreadFunc()
        {
            while (true)
            {
                try
                {
                    if (IsConnected & _netStream != null)
                    {
                        if (_netStream.CanRead && _netStream.DataAvailable)
                        {
                            lock (_readBufferSyncObject)
                            {
                                //Clear buffer. 
                                //TODO: May be we will not need to do it. Will see after use experiance
                                //for (int i = 0; i < _readBuffer.Length; i++)
                                //{
                                //    _readBuffer[i] = 0x00;
                                //}

                                int count = _netStream.Read(_readBuffer, 0, _readBuffer.Length);
                                if (count > 0)
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        _bytesQueue.Enqueue(_readBuffer[i]);
                                    }
                                    if (_bytesQueue.IsEmpty) _hasDataWh?.Reset();
                                    else _hasDataWh?.Set();
                                }
                                continue;
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    var ex = exception.InnerException as SocketException;
                    if (ex != null)
                    {
                        switch (ex.SocketErrorCode)
                        {
                            case SocketError.SocketError:
                            case SocketError.Fault:
                            case SocketError.NotSocket:
                            case SocketError.SocketNotSupported:
                            case SocketError.AddressNotAvailable:
                            case SocketError.NetworkDown:
                            case SocketError.NetworkUnreachable:
                            case SocketError.NetworkReset:
                            case SocketError.ConnectionAborted:
                            case SocketError.ConnectionReset:
                            case SocketError.NotConnected:
                            case SocketError.Shutdown:
                            case SocketError.TimedOut:
                            case SocketError.ConnectionRefused:
                            case SocketError.HostDown:
                            case SocketError.HostUnreachable:
                            case SocketError.SystemNotReady:
                            case SocketError.Disconnecting:
                            case SocketError.HostNotFound:
                            case SocketError.OperationAborted:
                                DisconnectFromServer();
                                continue;
                        }
                    }
                    OnErrorEvent(exception);
                }
            }
        }

        private void ProcessDataThreadFunc()
        {
            while (true)
            {
                try
                {
                    if (_hasDataWh != null && _hasDataWh.WaitOne(Timeout.Infinite))
                    {
                        byte dataByte;
                        if (_bytesQueue.TryDequeue(out dataByte))
                        {
                            ProcessByteInternal(dataByte);
                        }
                        if (_bytesQueue.IsEmpty) _hasDataWh?.Reset();
                        else _hasDataWh?.Set();
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnErrorEvent(ex);
                }
            }
        }

        #endregion

        #region Methods

        private bool ConnectToServer()
        {
            try
            {
                _client = new System.Net.Sockets.TcpClient(IpAddress, Port);
                _netStream = _client.GetStream();
                _netStream.WriteTimeout = Timeout.Infinite;
                _netStream.ReadTimeout = Timeout.Infinite;

                IsConnected = true;
                _pingFailCount = 0;
                OnConnectedEvent(_client.Client.LocalEndPoint, _client.Client.RemoteEndPoint);
                return true;
            }
            catch (SocketException se)
            {
                OnConnectionFailEvent(se.SocketErrorCode.ToString());
                return false;
            }
            catch (Exception ex)
            {
                OnConnectionFailEvent(ex.Message);
                return false;
            }
        }
        private void DisconnectFromServer()
        {
            if (_client != null)
            {
                if (IsConnected)
                {
                    _client?.Close();
                }
                _client = null;
                _netStream = null;
            }
            if (IsConnected)
            {
                IsConnected = false;
                OnDisconnectedEvent();
            }
        }

        /// <summary>
        /// Start client
        /// </summary>
        /// <param name="ipAddress">Remote server ip address</param>
        /// <param name="port"> Remote server port</param>
        public void Start(string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
            IsStarted = true;
        }

        /// <summary>
        /// Disconnect and stop cilent
        /// </summary>
        public void Stop()
        {
            IsStarted = false;
            IpAddress = string.Empty;
            Port = 0;
        }

        protected bool Write(byte[] data)
        {
            if (IsConnected & _netStream != null)
            {
                if (_netStream.CanWrite)
                {
                    _netStream.Write(data, 0, data.Length);
                    return true;
                }
            }
            return false;
        }


        #endregion

        protected abstract void ProcessByteInternal(byte dataByte);

        #region events
        /// <summary>
        /// Client connected
        /// </summary>
        public event EventHandler<ConnectionEventArgs> ConnectedEvent;
        protected void OnConnectedEvent(EndPoint localEndPoint, EndPoint remoteEndPoint)
        {
            ConnectedEvent?.Invoke(this, new ConnectionEventArgs(localEndPoint, remoteEndPoint));
        }

        public event EventHandler DisconnectedEvent;
        protected void OnDisconnectedEvent()
        {
            DisconnectedEvent?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ReconnectingEvent;
        protected void OnReconnectingEvent()
        {
            ReconnectingEvent?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<ConnectionFailEventArgs> ConnectionFailEvent;
        protected void OnConnectionFailEvent(string result)
        {
            ConnectionFailEvent?.Invoke(this, new ConnectionFailEventArgs(result));
        }

        public event EventHandler StartedEvent;
        private void OnStartedEvent()
        {
            StartedEvent?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler StoppedEvent;
        private void OnStoppedEvent()
        {
            StoppedEvent?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Dispodable
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connectThread != null)
                {
                    if (_connectThread.IsAlive)
                    {
                        _connectThread.Abort();
                    }
                }

                if (_readFromStreamThread != null)
                {
                    if (_readFromStreamThread.IsAlive)
                    {
                        _readFromStreamThread.Abort();
                    }
                }

                if (_processDataThread != null)
                {
                    if (_processDataThread.IsAlive)
                    {
                        _processDataThread.Abort();
                    }
                }

                DisconnectFromServer();

                ConnectedEvent = null;
                ConnectionFailEvent = null;
                DisconnectedEvent = null;
                ReconnectingEvent = null;
                StartedEvent = null;
                StoppedEvent = null;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
