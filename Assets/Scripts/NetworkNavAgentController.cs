using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NetworkNavAgentController : Networkable, IHaveNavAgent
{
    public NavMeshAgent myAgent;
    public CharacterName myCharacterName;
    public Animator myAnimator;

    new void Start()
    {
        base.Start();
    }

    void FixedUpdate()
    {
        myAnimator.SetFloat("WalkSpeed", myAgent.velocity.magnitude / myAgent.speed);
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
