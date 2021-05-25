using UnityEngine;
using UnityP2P;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    bool isServer = false;
    Server gameServer;

    Client gameClient;

    public static NetworkManager Instance { get; private set; }

    private INetworkManager _network;

    public Dictionary<string, Networkable> managedObjects { get; private set; } = new Dictionary<string, Networkable>();

    private Queue<Action> networkActions = new Queue<Action>();

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

    void Awake()
    {
        if (Instance is null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Debug.Log("Network manager already exists! Destroying this one");
            Destroy(this);
        }
    }

    void Start()
    {
        _network = new PassthoughClient();
    }

    public void StartServer(int port = 8675)
    {
        gameServer = new Server(port);
        gameServer.Start();
        isServer = true;
        _network = new NetworkManagerServer(gameServer);
        gameServer.OnDataReceived += Server_DataReceived;
    }

    public void StartClient(int port = 8676)
    {
        if (isServer)
        {
            throw new Exception("Cannot be a client and a server");
        }
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Loopback, 8675);
        gameClient = new Client(serverEndpoint, port);
        gameClient.Start();
        _network = new NetworkManagerClient(gameClient);
        gameClient.OnServerDataReceived += Client_DataReceived;
    }

    #region Client side code
    void Client_DataReceived(ServerPacket packet)
    {
        lock (networkActions)
        {
            networkActions.Enqueue(() =>
            {
                switch (packet.dataType)
                {
                    case PacketDataType.BeginConnection:
                        Client_ConnectedToServer();
                        return;

                    case PacketDataType.Transform:
                        Client_HandleObjectMove(packet);
                        return;

                    case PacketDataType.InstanciateObject:
                        Debug.Log("Received request to instanciate an object from the server");
                        Client_HandleObjectInstanciation(packet);
                        return;

                    default:
                        Debug.Log("Unable to determine packet type");
                        return;
                }
            });
        }
    }

    void Client_HandleObjectMove(ServerPacket packet)
    {
        ObjectMoveRequest omr = (ObjectMoveRequest)packet.data;
        if (managedObjects.ContainsKey(omr.objectId))
        {
            Networkable toMove = managedObjects[omr.objectId];
            if (toMove is IHaveNavAgent)
            {
                ((IHaveNavAgent)toMove).MoveAgent((Vector3)omr.transformData.position);
                return;
            }
            toMove.transform.position = (Vector3)omr.transformData.position;
            toMove.transform.rotation = (Quaternion)omr.transformData.quaternion;
        }
    }

    void Client_HandleObjectInstanciation(ServerPacket packet)
    {
        ObjectInstanciationRequest or = (ObjectInstanciationRequest)packet.data;
        switch (or.objectType)
        {
            case ObjectInstanciationType.Character:
                NetworkHelpers.InstanciateObject(or.positionData, ObjectInstanciationType.Character, CharacterName.NetworkLouise, or.objectId);
                return;
            default:
                return;
        }
    }

    void Client_ConnectedToServer()
    {
        // Tell server to instanciate our character
        var myCharacter = managedObjects.Where((obj) => obj.Value is IHaveNavAgent).First().Value;
        TransformData tData = new TransformData(myCharacter.transform.position, myCharacter.transform.rotation);
        ObjectInstanciationRequest or = new ObjectInstanciationRequest(myCharacter.Id, tData, ObjectInstanciationType.Character);
        ClientPacket cp = new ClientPacket(PacketDataType.InstanciateObject, or, gameClient.id);
        gameClient.SendData(Encoder.GetObjectBytes(cp));
    }

    public void RequestInstanciate(Networkable objectToInstanciate)
    {

    }

    #endregion

    #region Server side code
    void Server_DataReceived(ClientPacket packet)
    {
        lock (networkActions)
        {
            networkActions.Enqueue(() =>
            {
                Debug.Log($"Sever data received from client {packet.clientId}... {packet.dataType}");
                switch (packet.dataType)
                {
                    case PacketDataType.InstanciateObject:
                        Server_InstanciateObject(packet);
                        return;

                    case PacketDataType.BeginConnection:
                        Server_ClientConnected();
                        return;

                    default:
                        return;
                }
            });
        }
    }

    void Server_InstanciateObject(ClientPacket packet)
    {
        Debug.Log("Received request to instanciate the player");
        ObjectInstanciationRequest or = (ObjectInstanciationRequest)packet.data;
        switch (or.objectType)
        {
            case ObjectInstanciationType.Character:
                _network.Instanciate(or.positionData, or.objectType, CharacterName.Louise);
                Server_ClientConnected();
                return;
            default:
                return;
        }
    }

    void Server_ClientConnected()
    {
        foreach (var obj in managedObjects)
        {
            var val = obj.Value;
            // This is a character. Send character instanciation packet
            if (obj.Value is IHaveNavAgent)
            {
                Debug.Log("Client connected: Attempting to send server data to all other connected clients");
                TransformData tData = new TransformData(val.transform.position, val.transform.rotation);
                _network.Instanciate(tData, ObjectInstanciationType.Character, CharacterName.NetworkLouise, obj.Key);
            }
        }
    }

    #endregion

    public void ManageObject(Networkable objectToManage)
    {
        if (!managedObjects.ContainsKey(objectToManage.Id))
            managedObjects.Add(objectToManage.Id, objectToManage);
    }

    public void ForgetObject(Networkable toForget)
    {
        if (managedObjects.ContainsKey(toForget.Id))
            managedObjects.Remove(toForget.Id);
    }

    public void ForgetObject(string idOfNetworkable)
    {
        if (managedObjects.ContainsKey(idOfNetworkable))
            managedObjects.Remove(idOfNetworkable);
    }

    public void RequestMoveObject(Networkable requestingObject, Transform position)
    {
        RequestMoveObject(requestingObject, new TransformData(position.position, position.rotation));
    }

    public void RequestMoveObject(Networkable requestingObject, TransformData tData)
    {
        _network.MoveObjectTo(requestingObject, tData);
    }

    public void RequestMoveObject(Networkable requestingObject, Vector3 location)
    {
        RequestMoveObject(requestingObject, new TransformData(location));
    }

    void Update()
    {

    }

    void OnDestroy()
    {
        if (!(gameClient is null) && gameClient.Running)
        {
            gameClient.Stop();
        }
        if (!(gameServer is null) && gameServer.Running)
        {
            gameServer.Stop();
        }
    }
}

public interface INetworkManager
{
    public void MoveObjectTo(Networkable obj, TransformData transformData);

    public Networkable Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data, string key = null);
}

public interface IHaveNavAgent
{
    public void MoveAgent(Vector3 destination);

    public CharacterName GetCharacterName();
}


public abstract class Networkable : MonoBehaviour
{
    public string Id { get; set; }

    protected void Start()
    {
        if (Id is null)
            Id = System.Guid.NewGuid().ToString();
        NetworkManager.Instance.ManageObject(this);
    }

    void Destoy()
    {
        if (!(NetworkManager.Instance is null))
        {
            NetworkManager.Instance.ForgetObject(this);
        }
    }
}


#region INetworkManager implementations
public class NetworkManagerServer : INetworkManager
{
    Server networkServer;

    public NetworkManagerServer(Server currentServer)
    {
        networkServer = currentServer;
    }

    public void MoveObjectTo(Networkable obj, TransformData tData)
    {
        if (obj is IHaveNavAgent)
        {
            ((IHaveNavAgent)obj).MoveAgent((Vector3)tData.position);
        }
        ObjectMoveRequest omr = new ObjectMoveRequest(obj.Id, tData);
        ServerPacket movePacket = new ServerPacket(PacketDataType.Transform, omr);
        networkServer.SendData(movePacket);
    }

    public Networkable Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data, string key = null)
    {
        if (key is null)
            return NetworkHelpers.InstanciateObject(transformData, instanciationType, data);
        else
            return NetworkHelpers.InstanciateObject(transformData, instanciationType, data, key);

    }
}

public class NetworkManagerClient : INetworkManager
{
    Client networkClient;

    public NetworkManagerClient(Client currentClient)
    {
        networkClient = currentClient;
    }

    public void MoveObjectTo(Networkable obj, TransformData tData)
    {
        if (obj is IHaveNavAgent)
        {
            ((IHaveNavAgent)obj).MoveAgent((Vector3)tData.position);
        }
        else
        {
            obj.transform.position = (Vector3)tData.position;
        }
    }

    public Networkable Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data, string key = null)
    {
        if (key is null)
        {
            throw new Exception("Unable to instanciate object with no key");
        }
        if (NetworkManager.Instance.managedObjects.ContainsKey(key))
        {
            return null;
        }
        return NetworkHelpers.InstanciateObject(transformData, instanciationType, data, key);
    }
}

public class PassthoughClient : INetworkManager
{
    public void MoveObjectTo(Networkable obj, TransformData tData)
    {
        if (obj is IHaveNavAgent)
        {
            ((IHaveNavAgent)obj).MoveAgent((Vector3)tData.position);
        }
        else
        {
            obj.transform.position = (Vector3)tData.position;
        }
    }

    public Networkable Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data, string key = null)
    {
        return NetworkHelpers.InstanciateObject(transformData, instanciationType, data);
    }

}

#endregion

public static class NetworkHelpers
{
    public static Networkable InstanciateObject(TransformData location, ObjectInstanciationType instanciationType, object args)
    {
        switch (instanciationType)
        {
            case ObjectInstanciationType.Character:
                CharacterName name = CharacterName.NetworkLouise;
                var prefab = GameManager.Instance.characterPrefabs.Where((prefab) => prefab.characterName == name).First().prefab;
                var instanciated = GameObject.Instantiate(prefab, (Vector3)location.position, (Quaternion)location.quaternion);
                return instanciated.GetComponent<Networkable>();
            default:
                return null;
        }
    }

    public static Networkable InstanciateObject(TransformData location, ObjectInstanciationType instanciationType, object args, string key)
    {
        if (!NetworkManager.Instance.managedObjects.ContainsKey(key))
        {
            var networkObj = InstanciateObject(location, instanciationType, args);
            if (!(key is null))
            {
                networkObj.Id = key;
            }
            return networkObj;
        }
        return null;
    }
}