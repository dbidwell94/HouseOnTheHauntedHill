using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityP2P;
using System.Net;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    private INetworkManager _network = null;

    private Queue<Action> networkActions = new Queue<Action>();

    private HashSet<Networkable> networkables = new HashSet<Networkable>();

    void Awake()
    {
        if (Instance is null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("NetworkManager already exists, destorying this one");
            Destroy(this);
        }
        GameObject.DontDestroyOnLoad(this.gameObject);
    }

    void QueueAction(Action a)
    {
        lock (networkActions)
        {
            networkActions.Enqueue(a);
        }
    }

    void FixedUpdate()
    {
        lock (networkActions)
        {
            while (networkActions.Count > 0)
            {
                networkActions.Dequeue()();
            }
        }
    }

    public void RegisterNetworkable(Networkable nw)
    {
        if (!networkables.Contains(nw))
            networkables.Add(nw);

    }

    public void ForgetNetworkable(Networkable nw)
    {
        if (networkables.Contains(nw))
            networkables.Remove(nw);

    }

    public void StartServer(int port = 8675)
    {
        if (!(_network is null))
            return;

        var server = new Server(port);
        server.OnClientDataReceived += Server_DataReceived;
        server.OnClientConnected += Server_ClientConnected;
        server.OnClientDisconnect += (endpoint) => Debug.Log($"{endpoint.Address}:{endpoint.Port} has disconnected");
        _network = new NetworkServer(server);
    }


    public void StartClient(int serverPort = 8675)
    {
        if (!(_network is null))
            return;

        var client = new Client(serverPort);
        client.OnServerDataReceived += Client_DataReceived;
        _network = new NetworkClient(client);
    }


    void Server_DataReceived(ClientPacket cp)
    {
        QueueAction(() =>
        {
            switch (cp.dataType)
            {
                default:
                    return;
            }
        });
    }

    void Server_ClientConnected(IPEndPoint clientEndpoint)
    {
        QueueAction(() =>
        {
            foreach (var networkable in networkables)
            {
                Debug.Log($"Requesting {networkable.NetworkId} be instanciated on the client");
                _network.RequestInstanciate(clientEndpoint, networkable);
            }
        });
    }

    void Client_DataReceived(ServerPacket sp)
    {
        QueueAction(() =>
        {
            switch (sp.dataType)
            {
                case PacketDataType.InstanciateObject:
                    {
                        var or = (ObjectInstanciationRequest)sp.data;
                        Client_HandleServerInstanciationRequest(or);
                    }
                    return;

                case PacketDataType.Transform:
                    {
                        var or = (ObjectMoveRequest)sp.data;
                        Client_HandleServerMoveRequest(or);
                    }
                    return;

                default:
                    return;
            }
        });
    }

    void Client_HandleServerInstanciationRequest(ObjectInstanciationRequest or)
    {
        if (or.objectType == ObjectInstanciationType.Character)
        {
            var prefab = GameManager.Instance.characterPrefabs.Where((prefab) => prefab.characterName == CharacterName.NetworkLouise).First();
            var instanciated = Instantiate(prefab.prefab, (Vector3)or.positionData.position, (Quaternion)or.positionData.quaternion);
            instanciated.GetComponent<Networkable>().NetworkId = or.objectId;
        }
    }

    void Client_HandleServerMoveRequest(ObjectMoveRequest or)
    {
        throw new NotImplementedException();
    }


    public void KillServer()
    {
        _network?.KillServer();
    }

    void OnDestroy()
    {
        _network?.KillServer();
    }
}

interface INetworkManager
{
    void KillServer();

    void SendMessage(object message);

    void SendMessage(IPEndPoint endPoint, object message);

    void SyncNetworkableTransform(IPEndPoint endPoint, Networkable[] networkables);

    void RequestInstanciate(IPEndPoint endPoint, Networkable toInstanciate);

}

class NetworkServer : INetworkManager
{
    Server server;

    public NetworkServer(Server s)
    {
        server = s;
        server.Start();
        Debug.Log("Server started...");
    }

    public void KillServer()
    {
        server.Stop();
    }

    public void RequestInstanciate(IPEndPoint endPoint, Networkable toInstanciate)
    {
        ObjectInstanciationType instanciationType = (toInstanciate is IHaveNavAgent) ? ObjectInstanciationType.Character : ObjectInstanciationType.Room;

        ObjectInstanciationRequest or = new ObjectInstanciationRequest(
            toInstanciate.NetworkId,
            new TransformData(toInstanciate.transform.position, toInstanciate.transform.rotation),
            instanciationType);

        var sp = new ServerPacket(PacketDataType.InstanciateObject, or);
        server.SendPacket(endPoint, sp);
    }

    public void SendMessage(object data)
    {
        ServerPacket sp = new ServerPacket(PacketDataType.Message, data);
        server.SendPacketToAllClients(sp);
    }

    public void SendMessage(IPEndPoint endPoint, object message)
    {
        ServerPacket sp = new ServerPacket(PacketDataType.Message, message);
        server.SendPacket(endPoint, sp);
    }

    public void SyncNetworkableTransform(IPEndPoint endPoint, Networkable[] networkables)
    {
        throw new NotImplementedException();
    }
}

class NetworkClient : INetworkManager
{
    Client client;
    public NetworkClient(Client c)
    {
        client = c;
        client.Start();
    }

    public void KillServer()
    {
        client.Stop();
    }

    public void RequestInstanciate(IPEndPoint endPoint, Networkable toInstanciate)
    {
        throw new NotImplementedException();
    }

    public void SendMessage(object data)
    {

    }

    public void SendMessage(IPEndPoint endPoint, object message)
    {
        throw new NotImplementedException();
    }

    public void SyncNetworkableTransform(IPEndPoint endPoint, Networkable[] networkables)
    {
        throw new NotImplementedException();
    }
}

public abstract class Networkable : MonoBehaviour
{
    public string NetworkId = null;

    protected void Start()
    {
        if (NetworkId is null)
            NetworkId = System.Guid.NewGuid().ToString();
        NetworkManager.Instance.RegisterNetworkable(this);
    }

    protected void OnDestroy()
    {
        NetworkManager.Instance.ForgetNetworkable(this);
    }
}

public interface IHaveNavAgent
{
    void MoveAgent(Vector3 location);
}