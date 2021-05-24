using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    NavMeshAgent myAgent;
    public Animator myAnimator;

    // Start is called before the first frame update
    void Start()
    {
        myAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MovePlayer();
        }
    }

    void FixedUpdate()
    {
        myAnimator.SetFloat("WalkSpeed", myAgent.velocity.magnitude / myAgent.speed);
    }

    void MovePlayer()
    {
        RaycastHit hit;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.tag == "Floor")
        {
            myAgent.destination = new Vector3(hit.point.x, transform.position.y, hit.point.z);
        }
    }

}
