using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

namespace UnityP2P
{
    public class Client : IDisposable
    {
        private bool disposedValue;

        private Socket tcpClient;

        private IPEndPoint remoteServerEndpoint;

        private IPEndPoint clientEndpoint;

        private Thread clientThread;

        private bool shouldStop = false;

        private const int BUFFER_SIZE = 1024 * 1000 * 4;

        private byte[] buffer;

        public bool Running
        {
            get
            {
                return clientThread != null && clientThread.IsAlive;
            }
        }

        public Client(IPEndPoint remoteServerEndpoint, int clientPort = 8675)
        {
            this.remoteServerEndpoint = remoteServerEndpoint;
            this.clientEndpoint = new IPEndPoint(IPAddress.Any, clientPort);
            tcpClient = new Socket(this.clientEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            clientThread = new Thread(() =>
            {
                buffer = new byte[BUFFER_SIZE];
                ConnectToServer();

                while (!shouldStop)
                {
                    Thread.Sleep(1000);
                }
            });

            clientThread.Start();
        }

        public void Stop()
        {
            shouldStop = true;
        }



        void ConnectToServer()
        {
            try
            {
                tcpClient.Bind(clientEndpoint);
                tcpClient.Connect(remoteServerEndpoint);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Stop();
            }
            if (!shouldStop)
            {
                ListenForData();
            }
        }

        void ListenForData()
        {
            tcpClient.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, DataReceived, tcpClient);
        }

        void DataReceived(IAsyncResult AR)
        {
            Socket clientSocket = (Socket)AR.AsyncState;
            int bytesReceived = clientSocket.EndReceive(AR);
            if (bytesReceived == 0)
            {
                Debug.Log("Server disconnected...");
                Stop();
                return;
            }
            string builtString = "";
            for (int i = 0; i < bytesReceived; i++)
            {
                var currentByte = buffer[i];
                builtString += currentByte;
            }
            Debug.Log($"Data received from the server -- {builtString}");
            Array.Clear(buffer, 0, buffer.Length);
            ListenForData();
        }

        public void SendData(byte[] data)
        {
            if (!Running)
            {
                throw new Exception("Client is not running");
            }
            if (!tcpClient.Connected)
            {
                throw new Exception("Client is not connected to the server");
            }
            tcpClient.BeginSend(data, 0, data.Length, SocketFlags.None, DataSent, tcpClient);
        }

        void DataSent(IAsyncResult AR)
        {
            Socket clientSocket = (Socket)AR.AsyncState;
            var bytesSent = clientSocket.EndSend(AR);
            Debug.Log($"Sent {bytesSent} to the server....");
        }






        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}