﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using Flekosoft.Common.Network;
using Flekosoft.Common.Network.Tcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flekosoft.UnitTests.Common.Network
{
    [TestClass]
    public class TcpServerTests
    {
        [TestMethod]
        public void CreateStartStopDispose()
        {
            var server = new TcpServer();
            server.ErrorEvent += Server_ErrorEvent;
            Assert.IsFalse(server.DataTrace);
            Assert.IsFalse(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);

            ServerPropertyChangedEventArgs = null;
            StoppedEventColled = false;
            StartedEventColled = false;
            Assert.IsNull(ServerPropertyChangedEventArgs);
            Assert.IsFalse(StoppedEventColled);
            Assert.IsFalse(StartedEventColled);

            //Check just properties and events
            server.Start(new List<TcpServerLocalEndpoint>());
            Assert.IsFalse(server.DataTrace);
            Assert.IsTrue(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);

            Assert.IsNull(ServerPropertyChangedEventArgs);
            Assert.IsFalse(StoppedEventColled);
            Assert.IsFalse(StartedEventColled);
            server.Stop();

            Assert.IsFalse(server.DataTrace);
            Assert.IsFalse(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);

            Assert.IsNull(ServerPropertyChangedEventArgs);
            Assert.IsFalse(StoppedEventColled);
            Assert.IsFalse(StartedEventColled);

            server.PropertyChanged += Server_PropertyChanged;
            server.StartedEvent += Server_StartedEvent;
            server.StoppedEvent += Server_StoppedEvent;

            //Start with events
            server.Start(new List<TcpServerLocalEndpoint>());
            Assert.IsFalse(server.DataTrace);
            Assert.IsTrue(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);

            Assert.AreEqual("IsStarted", ServerPropertyChangedEventArgs?.PropertyName);
            Assert.IsFalse(StoppedEventColled);
            Assert.IsTrue(StartedEventColled);

            ServerPropertyChangedEventArgs = null;
            StoppedEventColled = false;
            StartedEventColled = false;
            Assert.IsNull(ServerPropertyChangedEventArgs);
            Assert.IsFalse(StoppedEventColled);
            Assert.IsFalse(StartedEventColled);

            server.Stop();
            Assert.IsFalse(server.DataTrace);
            Assert.IsFalse(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);

            Assert.AreEqual("IsStarted", ServerPropertyChangedEventArgs?.PropertyName);
            Assert.IsTrue(StoppedEventColled);
            Assert.IsFalse(StartedEventColled);

            ServerPropertyChangedEventArgs = null;
            Assert.IsNull(ServerPropertyChangedEventArgs);
            server.DataTrace = !server.DataTrace;
            Assert.AreEqual("DataTrace", ServerPropertyChangedEventArgs?.PropertyName);

            server.Start(new List<TcpServerLocalEndpoint>());
            server.Start(new List<TcpServerLocalEndpoint>());
            server.Dispose();
            Assert.IsTrue(server.DataTrace);
            Assert.IsFalse(server.IsStarted);
            Assert.IsTrue(server.IsDisposed);

        }

        [TestMethod]
        public void LocalhostServerStartedTests()
        {
            var server = new TcpServer();
            server.ErrorEvent += Server_ErrorEvent;

            server.StartListeningEvent += Server_StartListeningEvent;

            var epList = new List<TcpServerLocalEndpoint>();
            epList.Add(new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4444), 1));
            epList.Add(new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555), 1));
            epList.Add(new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6666), 1));

            EndPointArgs.Clear();
            Assert.AreEqual(0, EndPointArgs.Count);
            server.Start(epList);
            Assert.AreEqual(3, EndPointArgs.Count);
            Assert.AreEqual(epList[0].EndPoint, EndPointArgs[0].EndPoint);
            Assert.AreEqual(epList[1].EndPoint, EndPointArgs[1].EndPoint);
            Assert.AreEqual(epList[2].EndPoint, EndPointArgs[2].EndPoint);


            Assert.IsTrue(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);
            server.Dispose();
            Assert.IsTrue(server.IsDisposed);
            Assert.IsFalse(server.IsStarted);
        }

        [TestMethod]
        public void ConnectDisconnectServerTests()
        {
            var server = new TcpServer();
            server.ErrorEvent += Server_ErrorEvent;

            server.ConnectedEvent += Server_ConnectedEvent;
            server.DisconnectedEvent += Server_DisconnectedEvent;

            var epList = new List<TcpServerLocalEndpoint>();
            var ipEp1 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);
            var ipEp2 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            var ipEp3 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);

            epList.Add(new TcpServerLocalEndpoint(ipEp1, 1));
            epList.Add(new TcpServerLocalEndpoint(ipEp2, 1));
            epList.Add(new TcpServerLocalEndpoint(ipEp3, 1));

            server.Start(epList);

            var client = new TcpClient();

            ConnectionTest(client, ipEp1);
            ConnectionTest(client, ipEp2);
            ConnectionTest(client, ipEp3);

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client.Start(ipEp1);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp1, ConnectedEventArgs.LocalEndPoint);

            var remoteIp = ConnectedEventArgs.RemoteEndPoint;

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client.Dispose();
            Thread.Sleep(1500);
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNotNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp1, ServerDisconnectedEventArgs.LocalEndPoint);
            Assert.AreEqual(remoteIp, ServerDisconnectedEventArgs.RemoteEndPoint);

            var client1 = new TcpClient();
            var client2 = new TcpClient();
            var client3 = new TcpClient();

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client1.Start(ipEp1);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp1, ConnectedEventArgs.LocalEndPoint);

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client2.Start(ipEp2);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp2, ConnectedEventArgs.LocalEndPoint);

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client3.Start(ipEp3);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp3, ConnectedEventArgs.LocalEndPoint);


            client1.DisconnectedEvent += Client1_DisconnectedEvent;
            client2.DisconnectedEvent += Client2_DisconnectedEvent;
            client3.DisconnectedEvent += Client3_DisconnectedEvent;

            Client1DisconnectedEventColled = false;
            Client2DisconnectedEventColled = false;
            Client3DisconnectedEventColled = false;
            Assert.IsFalse(Client1DisconnectedEventColled);
            Assert.IsFalse(Client2DisconnectedEventColled);
            Assert.IsFalse(Client3DisconnectedEventColled);

            server.Stop();

            Thread.Sleep(1500);

            Assert.IsTrue(Client1DisconnectedEventColled);
            Assert.IsTrue(Client2DisconnectedEventColled);
            Assert.IsTrue(Client3DisconnectedEventColled);


            server.Start(epList);
            Thread.Sleep(1000);

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client1.Start(ipEp1);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp1, ConnectedEventArgs.LocalEndPoint);

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client2.Start(ipEp2);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp2, ConnectedEventArgs.LocalEndPoint);

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client3.Start(ipEp3);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(ipEp3, ConnectedEventArgs.LocalEndPoint);

            Client1DisconnectedEventColled = false;
            Client2DisconnectedEventColled = false;
            Client3DisconnectedEventColled = false;
            Assert.IsFalse(Client1DisconnectedEventColled);
            Assert.IsFalse(Client2DisconnectedEventColled);
            Assert.IsFalse(Client3DisconnectedEventColled);
            Assert.IsTrue(server.IsStarted);
            Assert.IsFalse(server.IsDisposed);
            server.Dispose();

            Thread.Sleep(1500);

            Assert.IsTrue(Client1DisconnectedEventColled);
            Assert.IsTrue(Client2DisconnectedEventColled);
            Assert.IsTrue(Client3DisconnectedEventColled);

            Assert.IsTrue(server.IsDisposed);
            Assert.IsFalse(server.IsStarted);
        }

        [TestMethod]
        public void ConnectionCountTest()
        {
            var server = new TcpServer();
            server.ErrorEvent += Server_ErrorEvent;

            server.ConnectedEvent += Server_ConnectedEvent;
            server.DisconnectedEvent += Server_DisconnectedEvent;

            var epList = new List<TcpServerLocalEndpoint>();
            var ipEp1 = (new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1111), 1));
            var ipEp2 = (new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2222), 2));
            var ipEp3 = (new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3333), 3));

            epList.Add(ipEp1);
            epList.Add(ipEp2);
            epList.Add(ipEp3);

            server.Start(epList);

            ConnectionTest(ipEp1);
            ConnectionTest(ipEp2);
            ConnectionTest(ipEp3);

            server.Dispose();
        }

        [TestMethod]
        public void SendReceiveTest()
        {
            var server = new TcpServer();
            server.ErrorEvent += Server_ErrorEvent;

            server.DataReceivedEvent += Server_DataReceivedEvent;

            var epList = new List<TcpServerLocalEndpoint>();
            var ipEp1 = (new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234), 1));

            epList.Add(ipEp1);
            server.Start(epList);

            var client = new TcpClient();
            client.DataReceivedEvent += Client_DataReceivedEvent;
            client.Start(ipEp1.EndPoint);
            Thread.Sleep(200);

            for (int i = 1; i < 10; i++)
            {
                var data = BitConverter.GetBytes(i);
                ServerDataReceivedEvent.Clear();
                Assert.AreEqual(0, ServerDataReceivedEvent.Count);
                client.SendData(data);
                Thread.Sleep(100);
                Assert.AreEqual(data.Length, ServerDataReceivedEvent.Count);
                for (int j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], ServerDataReceivedEvent[j].Data[0]);
                    Assert.AreEqual(client.ExchangeInterface.LocalEndPoint, ServerDataReceivedEvent[j].RemoteEndPoint);
                    Assert.AreEqual(client.ExchangeInterface.RemoteEndPoint, ServerDataReceivedEvent[j].LocalEndPoint);
                }
            }

            for (int i = 1; i < 10; i++)
            {
                var data = BitConverter.GetBytes(i);
                ClientDataReceivedEvent.Clear();
                Assert.AreEqual(0, ClientDataReceivedEvent.Count);
                server.Write(data, client.ExchangeInterface.RemoteEndPoint, client.ExchangeInterface.LocalEndPoint);
                Thread.Sleep(100);
                Assert.AreEqual(data.Length, ClientDataReceivedEvent.Count);
                for (int j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], ClientDataReceivedEvent[j].Data[0]);
                    Assert.AreEqual(ipEp1.EndPoint, ClientDataReceivedEvent[j].RemoteEndPoint);
                }
            }

            server.Dispose();
        }

        public List<NetworkDataEventArgs> ClientDataReceivedEvent { get; } = new List<NetworkDataEventArgs>();
        private void Client_DataReceivedEvent(object sender, NetworkDataEventArgs e)
        {
            ClientDataReceivedEvent.Add(e);
        }

        public List<NetworkDataEventArgs> ServerDataReceivedEvent { get; } = new List<NetworkDataEventArgs>();
        private void Server_DataReceivedEvent(object sender, NetworkDataEventArgs e)
        {
            ServerDataReceivedEvent.Add(e);
        }

        [TestMethod]
        public void DataTraceTest()
        {
            var server = new TcpServer();
            server.ErrorEvent += Server_ErrorEvent;

            server.ReceiveDataTraceEvent += Server_ReceiveDataTraceEvent;
            server.SendDataTraceEvent += Server_SendDataTraceEvent;
            server.DataReceivedEvent += Server_DataReceivedEvent;

            var epList = new List<TcpServerLocalEndpoint>();
            var ipEp1 = (new TcpServerLocalEndpoint(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234), 1));

            epList.Add(ipEp1);
            server.Start(epList);

            var client = new TcpClient();
            client.DataReceivedEvent += Client_DataReceivedEvent;
            client.ReceiveDataTraceEvent += Client_ReceiveDataTraceEvent;
            client.SendDataTraceEvent += Client_SendDataTraceEvent;

            client.Start(ipEp1.EndPoint);
            Thread.Sleep(200);

            client.DataTrace = false;
            server.DataTrace = false;

            for (int i = 1; i < 10; i++)
            {
                var data = BitConverter.GetBytes(i);
                ServerDataReceivedEvent.Clear();
                ClientSendDataTraceEvent.Clear();
                ServerReceiveDataTraceEvent.Clear();
                Assert.AreEqual(0, ServerDataReceivedEvent.Count);
                Assert.AreEqual(0, ClientSendDataTraceEvent.Count);
                Assert.AreEqual(0, ServerReceiveDataTraceEvent.Count);
                client.SendData(data);
                Thread.Sleep(100);
                Assert.AreEqual(data.Length, ServerDataReceivedEvent.Count);
                Assert.AreEqual(0, ClientSendDataTraceEvent.Count);
                Assert.AreEqual(0, ServerReceiveDataTraceEvent.Count);
                for (int j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], ServerDataReceivedEvent[j].Data[0]);
                    Assert.AreEqual(client.ExchangeInterface.LocalEndPoint, ServerDataReceivedEvent[j].RemoteEndPoint);
                    Assert.AreEqual(client.ExchangeInterface.RemoteEndPoint, ServerDataReceivedEvent[j].LocalEndPoint);
                }
            }

            for (int i = 1; i < 10; i++)
            {
                var data = BitConverter.GetBytes(i);
                ClientDataReceivedEvent.Clear();
                ServerSendDataTraceEvent.Clear();
                ClientReceiveDataTraceEvent.Clear();
                Assert.AreEqual(0, ClientDataReceivedEvent.Count);
                Assert.AreEqual(0, ServerSendDataTraceEvent.Count);
                Assert.AreEqual(0, ClientReceiveDataTraceEvent.Count);
                server.Write(data, client.ExchangeInterface.RemoteEndPoint, client.ExchangeInterface.LocalEndPoint);
                Thread.Sleep(100);
                Assert.AreEqual(data.Length, ClientDataReceivedEvent.Count);
                Assert.AreEqual(0, ServerSendDataTraceEvent.Count);
                Assert.AreEqual(0, ClientReceiveDataTraceEvent.Count);
                for (int j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], ClientDataReceivedEvent[j].Data[0]);
                    Assert.AreEqual(ipEp1.EndPoint, ClientDataReceivedEvent[j].RemoteEndPoint);
                }
            }


            client.DataTrace = true;
            server.DataTrace = true;

            for (int i = 1; i < 10; i++)
            {
                var data = BitConverter.GetBytes(i);
                ServerDataReceivedEvent.Clear();
                ClientSendDataTraceEvent.Clear();
                ServerReceiveDataTraceEvent.Clear();
                Assert.AreEqual(0, ServerDataReceivedEvent.Count);
                Assert.AreEqual(0, ClientSendDataTraceEvent.Count);
                Assert.AreEqual(0, ServerReceiveDataTraceEvent.Count);
                client.SendData(data);
                Thread.Sleep(100);
                Assert.AreEqual(data.Length, ServerDataReceivedEvent.Count);
                Assert.AreEqual(1, ClientSendDataTraceEvent.Count);
                Assert.AreEqual(1, ServerReceiveDataTraceEvent.Count);
                for (int j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], ServerDataReceivedEvent[j].Data[0]);
                    Assert.AreEqual(client.ExchangeInterface.LocalEndPoint, ServerDataReceivedEvent[j].RemoteEndPoint);
                    Assert.AreEqual(client.ExchangeInterface.RemoteEndPoint, ServerDataReceivedEvent[j].LocalEndPoint);

                    Assert.AreEqual(data[j], ClientSendDataTraceEvent[j].Data[0]);
                    Assert.AreEqual(client.ExchangeInterface.LocalEndPoint, ClientSendDataTraceEvent[j].RemoteEndPoint);
                    Assert.AreEqual(client.ExchangeInterface.RemoteEndPoint, ClientSendDataTraceEvent[j].LocalEndPoint);

                    Assert.AreEqual(data[j], ServerReceiveDataTraceEvent[j].Data[0]);
                    Assert.AreEqual(client.ExchangeInterface.LocalEndPoint, ServerReceiveDataTraceEvent[j].RemoteEndPoint);
                    Assert.AreEqual(client.ExchangeInterface.RemoteEndPoint, ServerReceiveDataTraceEvent[j].LocalEndPoint);
                }
            }

            for (int i = 1; i < 10; i++)
            {
                var data = BitConverter.GetBytes(i);
                ClientDataReceivedEvent.Clear();
                ServerSendDataTraceEvent.Clear();
                ClientReceiveDataTraceEvent.Clear();
                Assert.AreEqual(0, ClientDataReceivedEvent.Count);
                Assert.AreEqual(0, ServerSendDataTraceEvent.Count);
                Assert.AreEqual(0, ClientReceiveDataTraceEvent.Count);
                server.Write(data, client.ExchangeInterface.RemoteEndPoint, client.ExchangeInterface.LocalEndPoint);
                Thread.Sleep(100);
                Assert.AreEqual(data.Length, ClientDataReceivedEvent.Count);
                Assert.AreEqual(1, ServerSendDataTraceEvent.Count);
                Assert.AreEqual(1, ClientReceiveDataTraceEvent.Count);
                for (int j = 0; j < data.Length; j++)
                {
                    Assert.AreEqual(data[j], ClientDataReceivedEvent[j].Data[0]);
                    Assert.AreEqual(ipEp1.EndPoint, ClientDataReceivedEvent[j].RemoteEndPoint);

                    Assert.AreEqual(data[j], ServerSendDataTraceEvent[j].Data[0]);
                    Assert.AreEqual(ipEp1.EndPoint, ServerSendDataTraceEvent[j].RemoteEndPoint);

                    Assert.AreEqual(data[j], ClientReceiveDataTraceEvent[j].Data[0]);
                    Assert.AreEqual(ipEp1.EndPoint, ClientReceiveDataTraceEvent[j].RemoteEndPoint);
                }
            }

            server.Dispose();
        }

        public List<NetworkDataEventArgs> ClientSendDataTraceEvent { get; } = new List<NetworkDataEventArgs>();
        private void Client_SendDataTraceEvent(object sender, NetworkDataEventArgs e)
        {
            ClientSendDataTraceEvent.Add(e);
        }

        public List<NetworkDataEventArgs> ClientReceiveDataTraceEvent { get; } = new List<NetworkDataEventArgs>();
        private void Client_ReceiveDataTraceEvent(object sender, NetworkDataEventArgs e)
        {
            ClientReceiveDataTraceEvent.Add(e);
        }

        public List<NetworkDataEventArgs> ServerSendDataTraceEvent { get; } = new List<NetworkDataEventArgs>();
        private void Server_SendDataTraceEvent(object sender, NetworkDataEventArgs e)
        {
            ServerSendDataTraceEvent.Add(e);
        }

        public List<NetworkDataEventArgs> ServerReceiveDataTraceEvent { get; } = new List<NetworkDataEventArgs>();
        private void Server_ReceiveDataTraceEvent(object sender, NetworkDataEventArgs e)
        {
            ServerReceiveDataTraceEvent.Add(e);
        }

        void ConnectionTest(TcpServerLocalEndpoint tslep)
        {
            List<TcpClient> clientsList = new List<TcpClient>();

            TcpClient client;

            for (int i = 0; i < tslep.MaxClients; i++)
            {
                client = new TcpClient();
                client.ConnectionFailEvent += Client_ConnectionFailEvent;
                client.DisconnectedEvent += Client_DisconnectedEvent;
                clientsList.Add(client);
                ConnectedEventArgs = null;
                ConnectionFailEventArgs = null;
                ServerDisconnectedEventArgs = null;
                Assert.IsNull(ConnectedEventArgs);
                Assert.IsNull(ConnectionFailEventArgs);
                Assert.IsNull(ServerDisconnectedEventArgs);
                client.Start(tslep.EndPoint);
                Thread.Sleep(200);
                Assert.IsNull(ConnectionFailEventArgs);
                Assert.IsNotNull(ConnectedEventArgs);
                Assert.IsNull(ServerDisconnectedEventArgs);
                Assert.AreEqual(tslep.EndPoint, ConnectedEventArgs.LocalEndPoint);

            }

            client = new TcpClient();
            client.ConnectionFailEvent += Client_ConnectionFailEvent;
            client.DisconnectedEvent += Client_DisconnectedEvent;
            clientsList.Add(client);
            ConnectedEventArgs = null;
            ConnectionFailEventArgs = null;
            ClientDisconnectedEventColled = false;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ConnectionFailEventArgs);
            Assert.IsFalse(ClientDisconnectedEventColled);
            client.Start(tslep.EndPoint);
            Thread.Sleep(200);
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsTrue(ConnectionFailEventArgs != null || ClientDisconnectedEventColled);

            foreach (TcpClient tcpClient in clientsList)
            {
                tcpClient.Dispose();
            }
            clientsList.Clear();
        }

        public bool ClientDisconnectedEventColled { get; set; }
        private void Client_DisconnectedEvent(object sender, System.EventArgs e)
        {
            ClientDisconnectedEventColled = true;
        }

        public ConnectionFailEventArgs ConnectionFailEventArgs { get; set; }
        private void Client_ConnectionFailEvent(object sender, ConnectionFailEventArgs e)
        {
            ConnectionFailEventArgs = e;
        }

        public bool Client3DisconnectedEventColled { get; set; }
        private void Client3_DisconnectedEvent(object sender, System.EventArgs e)
        {
            Client3DisconnectedEventColled = true;
        }

        public bool Client2DisconnectedEventColled { get; set; }
        private void Client2_DisconnectedEvent(object sender, System.EventArgs e)
        {
            Client2DisconnectedEventColled = true;
        }

        public bool Client1DisconnectedEventColled { get; set; }
        private void Client1_DisconnectedEvent(object sender, System.EventArgs e)
        {
            Client1DisconnectedEventColled = true;
        }

        void ConnectionTest(TcpClient client, IPEndPoint endPoint)
        {
            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client.Start(endPoint);
            Thread.Sleep(200);
            Assert.IsNotNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(endPoint, ConnectedEventArgs.LocalEndPoint);

            var remoteIp = ConnectedEventArgs.RemoteEndPoint;

            ConnectedEventArgs = null;
            ServerDisconnectedEventArgs = null;
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNull(ServerDisconnectedEventArgs);
            client.Stop();
            Thread.Sleep(1500);
            Assert.IsNull(ConnectedEventArgs);
            Assert.IsNotNull(ServerDisconnectedEventArgs);
            Assert.AreEqual(endPoint, ServerDisconnectedEventArgs.LocalEndPoint);
            Assert.AreEqual(remoteIp, ServerDisconnectedEventArgs.RemoteEndPoint);
        }

        public ConnectionEventArgs ServerDisconnectedEventArgs { get; set; }
        private void Server_DisconnectedEvent(object sender, ConnectionEventArgs e)
        {
            ServerDisconnectedEventArgs = e;
        }

        public ConnectionEventArgs ConnectedEventArgs { get; set; }

        private void Server_ConnectedEvent(object sender, ConnectionEventArgs e)
        {
            ConnectedEventArgs = e;
        }

        private void Server_StartListeningEvent(object sender, EndPointArgs e)
        {
            EndPointArgs.Add(e);
        }

        public List<EndPointArgs> EndPointArgs { get; } = new List<EndPointArgs>();

        public bool StoppedEventColled { get; set; }

        private void Server_StoppedEvent(object sender, System.EventArgs e)
        {
            StoppedEventColled = true;
        }

        public bool StartedEventColled { get; set; }

        private void Server_StartedEvent(object sender, System.EventArgs e)
        {
            StartedEventColled = true;
        }

        private void Server_ErrorEvent(object sender, System.IO.ErrorEventArgs e)
        {
            Assert.Fail(e.GetException().ToString());
        }

        public PropertyChangedEventArgs ServerPropertyChangedEventArgs { get; set; }

        private void Server_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ServerPropertyChangedEventArgs = e;
        }
    }
}