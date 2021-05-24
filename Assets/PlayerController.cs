using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    NavMeshAgent myAgent;
    public Animator myAnimator;

    Vector3 previousPos;
    Quaternion previousRot;

    // Start is called before the first frame update
    void Start()
    {
        myAgent = GetComponent<NavMeshAgent>();
        previousPos = transform.position;
        previousRot = transform.rotation;
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
        if (previousPos != transform.position || previousRot != transform.rotation)
        {
            NetworkManager.Instance.UpdateGameObjectLocation(this.gameObject.transform);
        }
        previousPos = transform.position;
        previousRot = transform.rotation;
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
