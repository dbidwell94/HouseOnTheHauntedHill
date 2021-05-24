using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

namespace UnityP2P
{
    public class Client : IDisposable
    {
        #region Events

        public delegate void ClientDelegate(ServerPacket serverPacket);
        public event ClientDelegate OnServerDataReceived;

        #endregion


        public readonly string id;

        private Thread clientThread;

        private UdpClient udpClient;

        private IPEndPoint serverEndpoint;

        private bool serverSentResponse = false;

        private bool shouldStop = false;

        public bool Running
        {
            get
            {
                return clientThread != null && clientThread.IsAlive;
            }
        }

        public Client(IPEndPoint serverEndpoint, int port = 8676)
        {
            this.serverEndpoint = serverEndpoint;
            this.id = Guid.NewGuid().ToString();
            this.udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        }

        public void Start()
        {
            this.clientThread = new Thread(() =>
            {
                SendConnectionTest();
            });
            clientThread.Start();
        }

        public void Stop()
        {
            shouldStop = true;
        }

        private void SendConnectionTest()
        {
            int connectionAttempts = 0;
            udpClient.Connect(serverEndpoint);
            Task.Run(() =>
            {
                while (!serverSentResponse && !shouldStop)
                {
                    var dataToSend = new ClientPacket(PacketDataType.BeginConnection, null, id);
                    SendData(Encoder.GetObjectBytes(dataToSend));
                    Debug.Log($"Attempting Connection... {connectionAttempts}");
                    connectionAttempts++;
                    Thread.Sleep(1000);
                }
                Debug.Log("Server connection successful");
            });
            ListenForData();
        }

        public void SendData(byte[] data)
        {
            udpClient.BeginSend(data, data.Length, DataSent, udpClient);
        }

        private void DataSent(IAsyncResult re)
        {
            UdpClient client = (UdpClient)re.AsyncState;
            int bytesSent = client.EndSend(re);
            Debug.Log($"Sent {bytesSent} bytes to the server");
        }

        private void ListenForData()
        {
            while (Running && !shouldStop)
            {
                byte[] data = udpClient.Receive(ref serverEndpoint);
                if (!serverSentResponse)
                {
                    serverSentResponse = true;
                }
                ParseServerData(data);
            }
        }

        void ParseServerData(byte[] data)
        {
            ServerPacket packet = Encoder.GetServerPacket(data);
            OnServerDataReceived?.Invoke(packet);
        }

        public void Dispose()
        {
            Stop();
            udpClient.Close();
            udpClient.Dispose();
        }

    }
}