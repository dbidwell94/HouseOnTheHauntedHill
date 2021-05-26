using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NetworkNavAgentController : Networkable, IHaveNavAgent
{
    public NavMeshAgent myAgent;

    public CharacterName myCharacterName;

    new void Start()
    {
        base.Start();
    }

    public CharacterName GetCharacterName()
    {
        return CharacterName.NetworkLouise;
    }

    public void MoveAgent(Vector3 destination)
    {
        myAgent.SetDestination(destination);
    }
}
