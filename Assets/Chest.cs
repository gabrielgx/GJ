using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public Transform player;
    public Animator anim;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(player.position, this.transform.position) < 30)
        {
            Vector3 direction = player.position - this.transform.position;
            float angle = Vector3.Angle(direction, this.transform.forward);
            //if (Vector3.Distance(player.position, this.transform.position) < 10 && angle < 30)
            //{


                direction.y = 0;
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(direction), 0.1f);
                anim.SetBool("isIdle", false);
                if (direction.magnitude > 5)
                {
                    this.transform.Translate(0, 0, 0.05f);
                    anim.SetBool("isWalking", true);
                    anim.SetBool("isAttacking", false);
                }
                else
                {
                    anim.SetBool("isWalking", false);
                    anim.SetBool("isAttacking", true);
                }
            }
            else
            {
                anim.SetBool("isWalking", false);
                anim.SetBool("isAttacking", false);
                anim.SetBool("isIdle", true);
            }
        }

    //}
}
