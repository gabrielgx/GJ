using FGJ.UI;
using UnityEngine;

namespace FGJ.Interaction
{
    public class pickUp : MonoBehaviour, iInteraction
    {
        public item O_Item;
        [HideInInspector] public bool inRange;
        private GameObject player;
        private inventory Inventory;

        private void Start() 
        {
            Inventory = inventory.inventoryInstance;
            player = GameObject.FindGameObjectWithTag("Player");
        }

        public void interact()
        {
            if(inRange)
            {
                if(Inventory.addItem(O_Item, -1))
                {
                    Destroy(gameObject);
                }
            }
        }

        private void Update() 
        {
            if(player != null)
            {   
                if(Vector3.Distance(transform.position, player.transform.position) < 1.5f)
                {
                    inRange = true;
                }
                else
                {
                    inRange = false;
                }
            }
            if(inRange)
            {
                if(Inventory.addItem(O_Item, -1))
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
