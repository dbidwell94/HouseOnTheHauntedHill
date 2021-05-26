using UnityEngine;
using UnityEngine.AI;

public class PlayerController : Networkable, IHaveNavAgent
{
    public NavMeshAgent myAgent;
    public Animator myAnimator;

    private CharacterName myCharName;

    

    void Awake()
    {
        myCharName = CharacterName.Louise;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RequestMove();
        }
    }

    void FixedUpdate()
    {
        myAnimator.SetFloat("WalkSpeed", myAgent.velocity.magnitude / myAgent.speed);
    }

    void RequestMove()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.tag == "Floor")
        {
            NetworkManager.Instance.RequestMoveObject(this, new Vector3(hit.point.x, transform.position.y, hit.point.z));
        }
    }

    public void MoveAgent(Vector3 destination)
    {
        myAgent.SetDestination(destination);
    }

    public CharacterName GetCharacterName()
    {
        return myCharName;
    }
}
