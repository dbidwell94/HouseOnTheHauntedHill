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
    }

    public void StartClient(int port = 8676)
    {
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, 8675);
        gameClient = new Client(serverEndpoint, port);
        gameClient.Start();
    }

    public void UpdateGameObjectLocation(GameObject obj)
    {

    }

    void Update()
    {

    }
}
