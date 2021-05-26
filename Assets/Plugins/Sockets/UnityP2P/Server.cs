using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Open.Nat;

namespace UnityP2P
{
    public class Server
    {
        #region Events
        public delegate void ServerEvent(ClientPacket packet);
        public delegate void ServerEventEndpoint(IPEndPoint clientEndpoint);
        public event ServerEvent OnClientDataReceived;
        public event ServerEventEndpoint OnClientConnected;
        public event ServerEventEndpoint OnClientDisconnect;
        #endregion


        private TcpListener listener;

        private IPEndPoint serverEndpoint;

        private Dictionary<IPEndPoint, TcpClient> clients = new Dictionary<IPEndPoint, TcpClient>();

        public int ConnectedClients
        {
            get
            {
                return clients.Count;
            }
        }

        private const int CLIENT_BUFFER_LENGTH = 1024 * 1000;

        public Server(int port = 8675)
        {
            serverEndpoint = new IPEndPoint(IPAddress.Any, port);
            listener = new TcpListener(serverEndpoint);
        }

        public void Start()
        {
            listener.Start(16);

            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource(10000);
            discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts).ContinueWith((device) =>
            {
                device.Result?.CreatePortMapAsync(new Mapping(Protocol.Tcp, serverEndpoint.Port, serverEndpoint.Port, "House on the Haunted Hill")).ContinueWith((_) =>
                {
                    Debug.Log("Port mapping created");
                });
            });

            // Listen for connections
            listener.BeginAcceptTcpClient(TcpClientConnected, null);
        }

        public void Stop()
        {
            lock (clients)
            {
                foreach (var client in clients.Values)
                {
                    client.GetStream().Close();
                    client.Close();
                    client.Dispose();
                }
            }
            listener.Stop();
        }

        void TcpClientConnected(IAsyncResult ar)
        {
            TcpClient clientConnection = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptTcpClient(TcpClientConnected, null);
            IPEndPoint clientEndpoint = (IPEndPoint)clientConnection.Client.RemoteEndPoint;
            if (!clients.ContainsKey(clientEndpoint))
            {
                clients.Add(clientEndpoint, clientConnection);
            }
            Task.Run(() => ReceiveClientData(clientConnection));
            OnClientConnected?.Invoke(clientEndpoint);
        }

        void ReceiveClientData(TcpClient client)
        {
            byte[] clientBuffer = new byte[CLIENT_BUFFER_LENGTH];
            int bytesRead = 0;
            using (NetworkStream ns = new NetworkStream(client.Client))
            {
                while (client.Connected)
                {
                    bytesRead = ns.Read(clientBuffer, 0, clientBuffer.Length);
                    if (bytesRead == 0)
                        break;

                    byte[] data = new byte[bytesRead];
                    Array.Copy(clientBuffer, data, bytesRead);
                    Array.Clear(clientBuffer, 0, clientBuffer.Length);
                    ParseData(data);
                }
            }
            IPEndPoint clientEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
            DisconnectClient(clientEndpoint);
        }

        public void SendPacketToAllClients(ServerPacket sp)
        {
            Task.Run(() =>
            {
                lock (clients)
                {
                    foreach (var clientKV in clients)
                    {
                        var client = clientKV.Value;
                        if (!client.Connected) { DisconnectClient(clientKV.Key); continue; }
                        byte[] dataStream = Encoder.GetObjectBytes(sp);
                        client.GetStream().Write(dataStream, 0, dataStream.Length);
                    }
                }
            });
        }

        public void SendPacket(IPEndPoint endpoint, ServerPacket sp)
        {
            lock (clients)
            {
                if (clients.ContainsKey(endpoint) && clients[endpoint].Connected)
                {
                    Task.Run(() =>
                    {
                        byte[] dataStream = Encoder.GetObjectBytes(sp);
                        clients[endpoint].GetStream().Write(dataStream, 0, dataStream.Length);
                    });
                }
                else
                    Debug.LogError($"{endpoint.Address}:{endpoint.Port} is not available");
            }

        }

        void ParseData(byte[] data)
        {
            Encoder.GetClientPacket(data, out ClientPacket cp);
            if (cp is null)
                return;
            OnClientDataReceived?.Invoke(cp);
        }

        void DisconnectClient(IPEndPoint client)
        {
            lock (clients)
            {
                if (clients.ContainsKey(client))
                {
                    clients[client].Close();
                    clients[client].Dispose();
                    clients.Remove(client);
                }
            }
            OnClientDisconnect?.Invoke(client);
        }
    }
}