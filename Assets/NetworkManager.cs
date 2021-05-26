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

    /// <summary> Provides a way to look up a networkable by the networkable ID </summary>
    private Dictionary<string, Networkable> networkables = new Dictionary<string, Networkable>();

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
        Debug.Log($"Requesting to register networkable: {nw.NetworkId}");
        if (!networkables.ContainsKey(nw.NetworkId))
            networkables.Add(nw.NetworkId, nw);

    }

    public void ForgetNetworkable(Networkable nw)
    {
        if (nw.NetworkId is null)
            return;

        if (networkables.ContainsKey(nw.NetworkId))
            networkables.Remove(nw.NetworkId);

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
        client.OnServerConnected += Client_ServerConnected;
        client.OnServerDisconnected += (_) => KillServer();
        _network = new NetworkClient(client);
    }

    /// <summary>Event handler for data received event </summary>
    void Server_DataReceived(ClientPacket cp)
    {
        QueueAction(() =>
        {
            switch (cp.dataType)
            {
                case PacketDataType.InstanciateObject:
                    {
                        var or = (ObjectInstanciationRequest)cp.data;
                        Server_HandleInstanciationRequest(or);
                    }
                    return;

                case PacketDataType.Transform:
                    {
                        var or = (ObjectMoveRequest)cp.data;
                        Server_HandleClientMoveRequest(or);
                    }
                    return;

                default:
                    return;
            }
        });
    }

    void Server_HandleInstanciationRequest(ObjectInstanciationRequest or)
    {
        if (networkables.ContainsKey(or.objectId))
            return;

        var instanciatedNetworkable = InstanciateObjectLocally(or);
        _network.RequestInstanciate(null, instanciatedNetworkable);
    }

    void Server_HandleClientMoveRequest(ObjectMoveRequest or)
    {
        // TODO: logic to check if move is valid
        if (!networkables.ContainsKey(or.objectId))
            return;

        Networkable toMove = networkables[or.objectId];
        if (toMove is IHaveNavAgent)
        {
            ((IHaveNavAgent)toMove).MoveAgent((Vector3)or.transformData.position);
        }
        else
        {
            toMove.transform.position = (Vector3)or.transformData.position;
            toMove.transform.rotation = (Quaternion)or.transformData.quaternion;
        }

        _network.RequestMoveTo(toMove, (Vector3)or.transformData.position);
    }

    /// <summary>Event handler for the client connected event </summary>
    void Server_ClientConnected(IPEndPoint clientEndpoint)
    {
        QueueAction(() =>
        {
            foreach (var networkable in networkables.Values)
            {
                Debug.Log($"Requesting {networkable.NetworkId} be instanciated on the client");
                _network.RequestInstanciate(clientEndpoint, networkable);
            }
        });
    }

    /// <summary> Event handler for the data received event </summary>
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
        if (networkables.ContainsKey(or.objectId))
            return;

        InstanciateObjectLocally(or);
    }

    void Client_ServerConnected(IPEndPoint serverEndpoint)
    {
        QueueAction(() =>
        {
            var player = networkables.Values.Where((obj) => obj is IHaveNavAgent).First();
            if (!(player is null))
                _network.RequestInstanciate(serverEndpoint, player);
        });
    }

    void Client_HandleServerMoveRequest(ObjectMoveRequest or)
    {
        if (!networkables.ContainsKey(or.objectId))
            return;

        Networkable toMove = networkables[or.objectId];
        if (toMove is IHaveNavAgent)
        {
            ((IHaveNavAgent)toMove).MoveAgent((Vector3)or.transformData.position);
            return;
        }
        toMove.transform.position = (Vector3)or.transformData.position;
        toMove.transform.rotation = (Quaternion)or.transformData.quaternion;
    }

    Networkable InstanciateObjectLocally(ObjectInstanciationRequest or)
    {
        if (or.objectType == ObjectInstanciationType.Character)
        {
            var prefab = GameManager.Instance.characterPrefabs.Where((prefab) => prefab.characterName == CharacterName.NetworkLouise).First();
            var instanciated = Instantiate(prefab.prefab, (Vector3)or.positionData.position, (Quaternion)or.positionData.quaternion);
            instanciated.GetComponent<Networkable>().NetworkId = or.objectId;
            return instanciated.GetComponent<Networkable>();
        }
        return null;
    }

    public void RequestMoveObject(Networkable toMove, Vector3 moveTo)
    {
        if (_network is null)
        {
            if (toMove is IHaveNavAgent)
            {
                ((IHaveNavAgent)toMove).MoveAgent(moveTo);
                return;
            }
            toMove.transform.position = moveTo;
            return;
        }
        if (_network is NetworkServer)
        {
            var tData = new TransformData(moveTo);
            var or = new ObjectMoveRequest(toMove.NetworkId, tData);
            Client_HandleServerMoveRequest(or);
        }
        _network.RequestMoveTo(toMove, moveTo);
    }

    public void KillServer()
    {
        _network?.KillServer();
        _network = null;
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

    void RequestMoveTo(Networkable nw, Vector3 position);

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

    public void RequestInstanciate(IPEndPoint endpoint, Networkable toInstanciate)
    {
        ObjectInstanciationType instanciationType = (toInstanciate is IHaveNavAgent) ? ObjectInstanciationType.Character : ObjectInstanciationType.Room;

        ObjectInstanciationRequest or = new ObjectInstanciationRequest(
            toInstanciate.NetworkId,
            new TransformData(toInstanciate.transform.position, toInstanciate.transform.rotation),
            instanciationType);

        var sp = new ServerPacket(PacketDataType.InstanciateObject, or);

        if (!(endpoint is null))
            server.SendPacket(endpoint, sp);
        else
            server.SendPacketToAllClients(sp);
    }

    public void RequestMoveTo(Networkable nw, Vector3 position)
    {
        var tData = new TransformData(position);
        var or = new ObjectMoveRequest(nw.NetworkId, tData);
        server.SendPacketToAllClients(new ServerPacket(PacketDataType.Transform, or));
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
    string clientId;
    public NetworkClient(Client c)
    {
        clientId = Guid.NewGuid().ToString();
        client = c;
        client.Start();
    }

    public void KillServer()
    {
        client.Stop();
    }

    public void RequestInstanciate(IPEndPoint endPoint, Networkable toInstanciate)
    {
        var tData = new TransformData(toInstanciate.transform.position, toInstanciate.transform.rotation);
        var or = new ObjectInstanciationRequest(toInstanciate.NetworkId, tData, ObjectInstanciationType.Character);
        client.SendPacket(new ClientPacket(PacketDataType.InstanciateObject, or, clientId));
    }

    public void RequestMoveTo(Networkable nw, Vector3 position)
    {
        var tData = new TransformData(position);
        var or = new ObjectMoveRequest(nw.NetworkId, tData);
        client.SendPacket(new ClientPacket(PacketDataType.Transform, or, clientId));
    }

    public void SendMessage(object data)
    {
        throw new NotImplementedException();
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
    private string ___networkId = null;
    public string NetworkId
    {
        get
        {
            return ___networkId;
        }
        set
        {
            NetworkManager.Instance?.ForgetNetworkable(this);
            ___networkId = value;
            NetworkManager.Instance?.RegisterNetworkable(this);
        }
    }

    protected void Start()
    {
        if (NetworkId is null)
            NetworkId = System.Guid.NewGuid().ToString();
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