using UnityEngine;
using UnityP2P;
using System;
using System.Net;

public class NetworkManager : MonoBehaviour
{

    Server gameServer;

    Client gameClient;

    public static NetworkManager Instance { get; private set; }

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if (!Instance)
        {
            Instance = this;
        }
    }

    void Start()
    {

    }

    public void StartServer(int port = 8675)
    {
        gameServer = new Server(port);
        gameServer.Start();
        gameServer.OnDataReceived += ServerDataReceived;
    }

    public void StartClient(int port = 8676)
    {
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, 8675);
        gameClient = new Client(serverEndpoint, port);
        gameClient.Start();
        gameClient.OnServerDataReceived += ClientDataReceived;
    }

    void ServerDataReceived(ClientPacket packet)
    {
        Debug.Log($"Sever data received...");
    }

    void ClientDataReceived(ServerPacket packet)
    {
        Debug.Log($"Client data received...");
    }

    public void UpdateGameObjectLocation(Transform obj)
    {
        var tData = new TransformData(obj.position, obj.rotation);
        if (gameClient != null)
        {
            gameClient.SendData(Encoder.GetObjectBytes(new ClientPacket(PacketDataType.Transform, tData, gameClient.id)));
        }
        else if (gameServer != null)
        {

        }
    }

    void Update()
    {

    }

    void OnDestroy()
    {
        if (gameClient.Running)
        {
            gameClient.Stop();
        }
        if (gameServer.Running)
        {
            gameServer.Stop();
        }
    }
}
