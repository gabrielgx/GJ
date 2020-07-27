using FGJ.UI;
using Gamekit3D;
using UnityEngine;

namespace FGJ.Interaction
{
    public class pickUp : MonoBehaviour, iInteraction
    {
        public item O_Item;
        [HideInInspector] public bool inRange;
        private GameObject player;
        private inventory Inventory;
        private enum itemTypes
        {
            health, item, mission
        }
        [SerializeField] private itemTypes itemType;

        private void Start() 
        {
            Inventory = inventory.inventoryInstance;
            player = GameObject.FindGameObjectWithTag("Player");
        }

        public void interact()
        {
            if(inRange)
            {
                switch(itemType)
                {
                    case itemTypes.item:
                        if(Inventory.addItem(O_Item, -1))
                        {
                            Destroy(gameObject);
                        }
                    break;
                    case itemTypes.health:
                        player.GetComponent<Damageable>().heal(1);
                    break;
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
