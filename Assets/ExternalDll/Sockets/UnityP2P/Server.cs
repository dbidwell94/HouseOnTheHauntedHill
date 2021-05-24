using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace UnityP2P
{
    public class Server : IDisposable
    {
        #region events

        public delegate void UdpServerDelegate(ClientPacket packet);
        public event UdpServerDelegate OnDataReceived;

        #endregion

        private Thread serverThread;

        private Socket udpServer;

        private IPEndPoint serverEndpoint;

        private const int BUFFER_SIZE = 1024 * 1000 * 4;

        byte[] buffer;

        private bool shouldStop = false;

        Dictionary<string, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();

        public bool Running
        {
            get
            {
                return serverThread != null && serverThread.IsAlive;
            }
        }

        public Server(int port = 8675)
        {
            serverEndpoint = new IPEndPoint(IPAddress.Any, port);
            udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Start()
        {
            serverThread = new Thread(() =>
            {
                buffer = new byte[BUFFER_SIZE];
                udpServer.Bind(serverEndpoint);
                Debug.Log($"Server started at -- {serverEndpoint.Address}:{serverEndpoint.Port}");
                ReceiveData();
            });
            serverThread.Start();
        }

        public void Stop()
        {
            shouldStop = true;
        }

        void ReceiveData()
        {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

            EndPoint ep = (EndPoint)remoteEndpoint;

            while (Running && !shouldStop)
            {
                var receivedBytes = udpServer.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref ep);
                byte[] receivedDataArray = new byte[receivedBytes];
                Array.Copy(buffer, receivedDataArray, receivedBytes);
                Array.Clear(buffer, 0, buffer.Length);
                ParseByteData(receivedDataArray, (IPEndPoint)ep);
            }
        }

        public void SendData(ServerPacket packet, IPEndPoint ep)
        {
            udpServer.Connect(ep);
            udpServer.Send(Encoder.GetObjectBytes(packet));
        }

        void ParseByteData(byte[] data, IPEndPoint clientEndpoint)
        {
            ClientPacket packet = Encoder.GetClientPacket(data);

            if (!clients.ContainsKey(packet.clientId))
            {
                clients.Add(packet.clientId, clientEndpoint);
            }
            if (packet.dataType == PacketDataType.BeginConnection)
            {
                SendData(new ServerPacket(PacketDataType.BeginConnection, packet.clientId), clientEndpoint);
            }
            OnDataReceived?.Invoke(packet);
        }

        public void Dispose()
        {
            Stop();
            udpServer.Disconnect(false);
            udpServer.Dispose();
        }
    }
}