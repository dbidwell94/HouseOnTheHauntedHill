using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

namespace UnityP2P
{
    public class Client
    {
        #region Events

        public delegate void ClientEvent(ServerPacket packet);
        public delegate void ClientEventNoData();
        public event ClientEvent OnServerDataReceived;
        public event ClientEventNoData OnServerConnected;

        #endregion


        private TcpClient sender;
        private IPEndPoint serverEndpoint;

        private byte[] readBuffer;

        private const int CLIENT_BUFFER_LENGTH = 1024 * 1000;

        public Client(int serverPort = 8675)
        {
            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Any, 0);
            sender = new TcpClient(localEndpoint);
            this.serverEndpoint = new IPEndPoint(IPAddress.Any, serverPort);
            readBuffer = new byte[CLIENT_BUFFER_LENGTH];
        }

        public void Start()
        {
            sender.Connect(serverEndpoint);
            OnServerConnected?.Invoke();
            Task.Run(() => Transmit());
        }

        public void Stop()
        {
            if (sender.Connected)
            {
                sender.GetStream().Close();
                sender.Close();
            }
            sender.Dispose();
        }

        void Transmit()
        {
            var stream = sender.GetStream();
            ReadData(stream);
        }

        public void SendPacket(ClientPacket cp)
        {
            Task.Run(() =>
            {
                byte[] dataStream = Encoder.GetObjectBytes(cp);
                sender.GetStream().Write(dataStream, 0, dataStream.Length);
            });
        }

        void ReadData(NetworkStream stream)
        {
            while (sender.Connected)
            {
                int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                Debug.Log($"Client received data: {bytesRead} bytes");
                byte[] data = new byte[bytesRead];
                Array.Copy(readBuffer, data, bytesRead);
                Array.Clear(readBuffer, 0, readBuffer.Length);
                ParseData(data);
            }
        }

        void ParseData(byte[] data)
        {
            Encoder.GetServerPacket(data, out ServerPacket sp);
            if (sp is null)
                return;
            OnServerDataReceived?.Invoke(sp);
        }
    }
}