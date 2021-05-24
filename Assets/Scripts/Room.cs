using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum RoomFloor
{
    Basement,
    Main,
    Upper
}

public enum RoomDoorLocation
{
    Left,
    Right,
    Front,
    Back
}

public struct RoomEventArgs
{
    public Room sender;
    public GameObject triggeredObject;
    public RoomDoorLocation doorLocation;

    public RoomEventArgs(Room sender, GameObject triggeredObject, RoomDoorLocation doorLocation)
    {
        this.sender = sender;
        this.triggeredObject = triggeredObject;
        this.doorLocation = doorLocation;
    }
}

public delegate void RoomTriggerEventHandler(RoomEventArgs sender);

public class Room : MonoBehaviour
{
    #region Constants
    private const string TriggerLeft = "Door Trigger Left";
    private const string TriggerRight = "Door Trigger Right";
    private const string TriggerFront = "Door Trigger Front";
    private const string TriggerBack = "Door Trigger Back";
    #endregion

    #region Properties
    public GameObject[] RoomElements { get; private set; }

    public NavMeshLink FloorSurfaceLink;
    public bool DoorBack { get; private set; }
    public bool DoorFront { get; private set; }
    public bool DoorRight { get; private set; }
    public bool DoorLeft { get; private set; }
    public RoomFloor CurrentFloor { get; private set; }
    #endregion

    #region Events
    public static event RoomTriggerEventHandler OnDoorwayEnter;
    #endregion

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public Vector3 GetRoomLocationAdjacentToDoor(RoomDoorLocation doorLocation)
    {
        Vector3 currentPos = gameObject.transform.position;
        switch (doorLocation)
        {
            case RoomDoorLocation.Front:
                return new Vector3(currentPos.x, currentPos.y, currentPos.z + GameManager.Instance.DefaultRoomSize.x);
            case RoomDoorLocation.Back:
                return new Vector3(currentPos.x, currentPos.y, currentPos.z - GameManager.Instance.DefaultRoomSize.x);
            case RoomDoorLocation.Right:
                return new Vector3(currentPos.x + GameManager.Instance.DefaultRoomSize.x, currentPos.y, currentPos.z);
            case RoomDoorLocation.Left:
                return new Vector3(currentPos.x - GameManager.Instance.DefaultRoomSize.x, currentPos.y, currentPos.z);
            default:
                throw new Exception("Unable to determine door location");
        }
    }

    public void RebuildNavData()
    {
        FloorSurfaceLink.UpdateLink();
    }


    public void OnDoorTriggerEnter(Collider other, RoomDoorLocation doorLocation)
    {
        OnDoorwayEnter?.Invoke(new RoomEventArgs(this, other.gameObject, doorLocation));
    }
}
