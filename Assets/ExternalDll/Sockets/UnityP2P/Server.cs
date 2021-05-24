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
        private IPEndPoint serverEndpoint;

        private Socket tcpServer;

        const int BUFFER_SIZE = 1024 * 1000 * 4;

        HashSet<Socket> connectedClients = new HashSet<Socket>();

        public int MaxConnections { get; private set; }

        byte[] buffer;

        Thread serverThread;

        public Server(int port = 8675, int maxConnections = 8)
        {
            serverEndpoint = new IPEndPoint(IPAddress.Any, port);
            tcpServer = new Socket(serverEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            MaxConnections = maxConnections;
        }

        public void Start()
        {
            serverThread = new Thread(() =>
            {
                tcpServer.Bind(serverEndpoint);
                tcpServer.Listen(16);
                Debug.Log($"Game server started at -- {serverEndpoint.Address}:{serverEndpoint.Port}");
                buffer = new byte[BUFFER_SIZE];
                AcceptConnections();
                while (true)
                {
                    Thread.Sleep(1000);
                }
            });

            serverThread.Start();
        }

        void AcceptConnections()
        {
            tcpServer.BeginAccept(ConnectionReceived, tcpServer);
            Debug.Log("Started Accepting new connections...");
        }

        void ConnectionReceived(IAsyncResult res)
        {
            Socket clientSocket = ((Socket)res.AsyncState).EndAccept(res);
            IPEndPoint clientEndpoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            Debug.Log($"Client connected -- {clientEndpoint.Address}:{clientEndpoint.Port}");
            if (connectedClients.Count < MaxConnections)
            {
                connectedClients.Add(clientSocket);
                ListenForData(clientSocket);
            }
            AcceptConnections();
        }

        void ListenForData(Socket clientSocket)
        {
            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ClientDataReceived, clientSocket);
        }

        void ClientDataReceived(IAsyncResult res)
        {
            Socket clientSocket = (Socket)res.AsyncState;
            int bytesRead = clientSocket.EndReceive(res);

            if (bytesRead == 0)
            {
                Array.Clear(buffer, 0, buffer.Length);
                CloseClient(clientSocket);
                return;
            }

            string builtString = "";

            for (int i = 0; i < bytesRead; i++)
            {
                var currentByte = buffer[i];
                builtString += currentByte;
            }
            Array.Clear(buffer, 0, buffer.Length);
            Debug.Log($"Data received: {builtString}");
            ListenForData(clientSocket);
        }

        public void SendData(byte[] data)
        {
            foreach (var client in connectedClients)
            {
                client.BeginSend(data, 0, data.Length, SocketFlags.None, DataSent, client);
            }
        }

        void DataSent(IAsyncResult AR)
        {
            Socket clientSocket = (Socket)AR.AsyncState;

            IPEndPoint clientEndpoint = (IPEndPoint)clientSocket.RemoteEndPoint;

            var bytesSent = clientSocket.EndSend(AR);
            Debug.Log($"Sent {bytesSent} to the {clientEndpoint.Address}:{clientEndpoint.Port}....");
        }

        void CloseClient(Socket clientSocket)
        {
            IPEndPoint clientEndpoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            connectedClients.Remove(clientSocket);
            clientSocket.Dispose();
            Debug.Log($"Client {clientEndpoint.Address}:{clientEndpoint.Port} has disconnected");
        }

        public void Dispose()
        {
            tcpServer.Close();
            tcpServer.Dispose();
        }
    }
}