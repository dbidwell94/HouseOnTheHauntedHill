using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Room Card", menuName = "Cards/Room")]
public class RoomCard : ScriptableObject
{
    public GameObject roomPrefab;
    public new string name;
    public RoomFloor[] roomFloors;

    // TODO: Add fields for optional events, omens, and items
}
