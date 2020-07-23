using System.Collections;
using System.Collections.Generic;
using FGJ.Movement;
using FGJ.Core;
using FGJ.Interaction;
using UnityEngine;

namespace FGJ.control
{
    public class playerController : MonoBehaviour, Icontroller
    {
        [SerializeField] float movSpeed;
        [SerializeField] float jumpHeight = 1f;
        private Vector3 movDirection;
        private void Update()
        {
            movDirection = Vector3.zero;
            if(Manager.instance.playerCanMove)
            {
                float ZAxis = Input.GetAxis("Vertical");
                float XAxis = Input.GetAxis("Horizontal");
                movDirection = new Vector3(XAxis, 0, ZAxis);

                if(Input.GetKeyDown(KeyCode.Space))
                {
                    jump();
                }

                if(Input.GetKeyDown(KeyCode.E))
                {
                    interaction();
                }
            }
            GetComponent<movement>().velocity.x = movDirection.x * movSpeed;
            GetComponent<movement>().velocity.z = movDirection.z * movSpeed;
             
            if(Input.GetKeyDown(KeyCode.I))
            {
                openInventory();
            }
        }
        private void jump()
        {
            GetComponent<movement>().jump(jumpHeight);
        }
        private void openMenu()
        {

        }
        private void openInventory()
        {
            if(!Manager.instance.inventaryOpen)
            {
                Manager.instance.openInventory();
            }
            else
            {
                Manager.instance.closeInventory();
            }
        }
        private void interaction()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out hit, 1000f);
            if(hit.collider.gameObject.GetComponent<iInteraction>() != null)
            {
                hit.collider.gameObject.GetComponent<iInteraction>().interact();
            }
        }
        public void died()
        {
            Manager.instance.playerCanMove = false;
        }
    }
}
