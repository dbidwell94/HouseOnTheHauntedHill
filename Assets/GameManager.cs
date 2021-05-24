using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public RoomCard[] roomPrefabs;

    public Vector2 DefaultRoomSize { get; } = new Vector2(11, 4);

    private Dictionary<string, GameObject> mainFloorRooms = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> basementFloorRooms = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> upperFloorRooms = new Dictionary<string, GameObject>();

    public static GameManager Instance { get; private set; } = null;

    public Dictionary<Vector3, GameObject> Rooms { get; private set; } = new Dictionary<Vector3, GameObject>();

    /*
    * Private properties
    */
    private Queue<RoomCard> availableRooms;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        GameObject.DontDestroyOnLoad(this.gameObject);
        availableRooms = Randomizer.RandomizeToQueue(roomPrefabs);
    }

    // Start is called before the first frame update
    void Start()
    {
        Room.OnDoorwayEnter += HandleDoorwayTriggers;
        foreach (var room in FindObjectsOfType<Room>())
        {
            var roomPosition = room.gameObject.transform.position;
            Rooms.Add(roomPosition, room.gameObject);
            room.RebuildNavData();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void RebuildNavMesh()
    {
        foreach (var room in Rooms)
        {
            room.Value.GetComponent<Room>().RebuildNavData();
        }
    }

    void HandleDoorwayTriggers(RoomEventArgs eventArgs)
    {
        var adjacentVectorToRoom = eventArgs.sender.GetRoomLocationAdjacentToDoor(eventArgs.doorLocation);
        if (!Rooms.ContainsKey(adjacentVectorToRoom))
        {
            throw new NotImplementedException();
        }
    }
}

public static class Randomizer
{
    public static T[] Randomize<T>(T[] toRandomize)
    {
        System.Random rand = new System.Random();
        return toRandomize.OrderBy(toRandomize => rand.Next()).ToArray();
    }

    public static Queue<T> RandomizeToQueue<T>(T[] toRandomize)
    {
        return new Queue<T>(toRandomize);
    }
}