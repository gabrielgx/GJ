using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

namespace FGJ.Movement
{
    public class movement : MonoBehaviour
    {
        [SerializeField] bool useGravity = true;
        [SerializeField] float gravity = -9.81f;
        [HideInInspector] public Vector3 velocity;
        private CharacterController controller;

        private void Start() 
        {
            controller = GetComponent<CharacterController>();
        }

        public void move(float speed, Vector3 direction)
        {
            controller.Move(direction * speed);
        }

        public void AIMove(Vector3 targetLocation)
        {   
            GetComponent<NavMeshAgent>().isStopped = false;
            GetComponent<NavMeshAgent>().SetDestination(targetLocation);
        }

        public void AIStop()
        {
            GetComponent<NavMeshAgent>().isStopped = true;
        }

        public void jump(float jumpHeight)
        {
            if(controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
            }
        }

        void FixedUpdate() 
        {
            if(controller != null)
            {
                if(useGravity)
                {
                    if(controller.isGrounded && velocity.y < 0)
                    {
                        velocity.y = -2f;
                    }
                    velocity.y += gravity * Time.deltaTime;
                }
                controller.Move(velocity * Time.fixedDeltaTime);
            }
        }
    }
}
