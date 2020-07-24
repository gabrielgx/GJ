using System.Collections;
using System.Collections.Generic;
using FGJ.Core;
using UnityEngine;

namespace FGJ.Combat
{
    public class dropItem : MonoBehaviour
    {
        [SerializeField] GameObject itemDrop;
        [SerializeField] float chanceToDrop;
        private bool dead;

        public void drop()
        {
            if(itemDrop != null)
            {
                if(Random.Range(1, 101) <= chanceToDrop)
                {
                    Instantiate(itemDrop, transform.position, Quaternion.identity);
                }
            }
        }
        private void Update() 
        {
            if(!dead)
            {
                if(Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) < 15f)
                {
                    Manager.instance.setCombat(true);
                }
                else
                {
                    //stopCombat();
                }  
            }  
        }
        public void stopCombat()
        {
            Manager.instance.setCombat(false);
        }
        public void setDead()
        {
            dead = true;
        }
    }
}
