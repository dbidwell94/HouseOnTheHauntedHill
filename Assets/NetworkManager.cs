using UnityEngine;
using UnityP2P;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;

public class NetworkManager : MonoBehaviour
{
    bool isServer = false;
    Server gameServer;

    Client gameClient;

    public static NetworkManager Instance { get; private set; }

    private INetworkManager _network;

    Dictionary<string, Networkable> managedObjects = new Dictionary<string, Networkable>();

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
        switch (packet.dataType)
        {
            case PacketDataType.BeginConnection:
                Client_ConnectedToServer();
                return;

            default:
                Debug.Log("Unable to determine packet type");
                return;
        }
    }

    void Client_ConnectedToServer()
    {

    }

    public void RequestInstanciate(Networkable objectToInstanciate)
    {

    }

    #endregion

    #region Server side code
    void Server_DataReceived(ClientPacket packet)
    {
        Debug.Log($"Sever data received...");
    }
    #endregion

    public void ManageObject(Networkable objectToManage)
    {
        if (!managedObjects.ContainsKey(objectToManage.id))
            managedObjects.Add(objectToManage.id, objectToManage);
    }

    public void ForgetObject(Networkable toForget)
    {
        if (managedObjects.ContainsKey(toForget.id))
            managedObjects.Remove(toForget.id);
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

    public void Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data);
}

public interface IHaveNavAgent
{
    public void MoveAgent(Vector3 destination);

    public CharacterName GetCharacterName();
}


public abstract class Networkable : MonoBehaviour
{
    public string id { get; private set; }

    protected void Start()
    {
        id = System.Guid.NewGuid().ToString();
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
        throw new NotImplementedException();
    }

    public void Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data)
    {

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

    public void Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data)
    {

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

    public void Instanciate(TransformData transformData, ObjectInstanciationType instanciationType, object data)
    {
        NetworkHelpers.InstanciateObject(transformData, instanciationType, data);
    }

}

#endregion

public static class NetworkHelpers
{
    public static void InstanciateObject(TransformData location, ObjectInstanciationType instanciationType, object args)
    {
        switch (instanciationType)
        {
            case ObjectInstanciationType.Character:
                CharacterName name = (CharacterName)args;
                var prefab = GameManager.Instance.characterPrefabs.Where((prefab) => prefab.characterName == name).First().prefab;
                GameObject.Instantiate(prefab, (Vector3)location.position, (Quaternion)location.quaternion);
                return;
            default:
                return;
        }
    }
}