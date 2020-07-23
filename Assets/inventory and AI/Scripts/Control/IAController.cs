using UnityEngine;
using UnityEngine.AI;
using FGJ.Movement;
using FGJ.Combat;

namespace FGJ.control
{
    public class IAController : MonoBehaviour, Icontroller
    {
        private bool wandering;
        private bool attacking;
        private float wanderTimer;
        private bool returning;
        private bool inArea;
        private Vector3 p_wanderRadiousCenter;
        private float p_wanderArea;
        private health target;
        [SerializeField] bool followLeader;
        [SerializeField] Transform packLeader;
        [SerializeField] float maxDistanceToLeader;
        [SerializeField] Transform wanderRadiousCenter;
        [SerializeField] float wanderArea;
        [SerializeField] float wanderTimeStoped = 0.5f;
        [SerializeField] float wanderMaxDistance = 5f;
        [SerializeField] float targetRadious = 8f;
        [SerializeField] float fleeRadious = 15f;
        [SerializeField] float attackRange = 2.5f;

        private void Start() 
        {
            wandering = true;
        }

        private void Update() 
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            p_wanderRadiousCenter = wanderRadiousCenter.position;
            p_wanderArea = wanderArea;
            if(followLeader)
            {
                if(packLeader != null)
                {
                    p_wanderRadiousCenter = packLeader.position;
                    p_wanderArea = maxDistanceToLeader;
                }
            }
            if(Vector3.Distance(transform.position, player.transform.position) < targetRadious)
            {
                target = player.GetComponent<health>();
            }
            else if(Vector3.Distance(transform.position, player.transform.position) > fleeRadious)
            {
                target = null;
            }
            if(target != null)
            {
                attacking = true;
                wandering = false;
            }
            else
            {
                attacking = false;
                wandering = true;
            }
            if(Vector3.Distance(transform.position, p_wanderRadiousCenter) < p_wanderArea)
            {
                inArea = true;
            }
            else
            {
                inArea = false;
            }
            print(inArea + gameObject.name);
            if(wandering)
            {   
                if(returning)
                {
                    GetComponent<NavMeshAgent>().speed = 2.5f;
                }
                else
                {
                    GetComponent<NavMeshAgent>().speed = 1f;
                }
                wander();
            }
            if(attacking)
            {
                GetComponent<NavMeshAgent>().speed = 3.5f;
                attack(target);
            }
        }

        private void wander()
        {
            if(inArea)
            {
                if(!GetComponent<NavMeshAgent>().hasPath)
                {
                    wanderTimer += Time.deltaTime;
                }
                if(wanderTimer > wanderTimeStoped)
                {
                    GetComponent<movement>().AIMove(randomizeLocation(transform.position, wanderMaxDistance, -1));
                    wanderTimer = 0f;
                }
            }
            if(!inArea)
            {
                GetComponent<movement>().AIMove(p_wanderRadiousCenter);
                returning = true;
            }
            if(returning && inArea)
            {
                GetComponent<movement>().AIMove(transform.position);
                returning = false;
            }
        }

        private void attack(health atkTarget)
        {
            if(Vector3.Distance(transform.position, atkTarget.transform.position) < attackRange)
            {
                GetComponent<movement>().AIStop();
                print("Atacando");
            }
            else
            {
                GetComponent<movement>().AIMove(atkTarget.transform.position);
            }
        }

        private Vector3 randomizeLocation(Vector3 origin, float distace, LayerMask lMask)
        {
            Vector3 randomDirection = Random.insideUnitSphere * distace;
            randomDirection += origin;
            NavMeshHit navHit;
            NavMesh.SamplePosition(randomDirection, out navHit, distace, lMask);
            if(Vector3.Distance(navHit.position, p_wanderRadiousCenter) < wanderArea)
            {
                return navHit.position;
            }
            else
            {
                return transform.position;
            }
        }

        public void died()
        {

        }
    }
}