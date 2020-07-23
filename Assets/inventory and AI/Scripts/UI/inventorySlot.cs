using UnityEngine.UI;
using UnityEngine;
using FGJ.Core;
using Gamekit3D;
using FGJ.Combat;

namespace FGJ.UI
{
    public class inventorySlot : MonoBehaviour
    {
        private GameObject currentItemSprite;
        [SerializeField] inventory inventoryInstance;
        [HideInInspector] public bool usingItem;
        [HideInInspector] public inventorySlot cameFrom;
        [HideInInspector] public inventorySlot usingIn;
        public bool craftslot;
        public bool resultSlot;
        public bool gunSlot;
        public bool meleeSlot;
        [SerializeField] GameObject XBtn;
        [HideInInspector] public bool empty = true;
        [HideInInspector] public int slotIndex;
        [HideInInspector] public item currentItem;

        private void Start() 
        {
            if(gunSlot)
            {
                if(empty)
                {
                    Manager.instance.playerCanShot = false;
                }
                else
                {
                    Manager.instance.playerCanShot = true;
                }
            }
            if(meleeSlot)
            {
                if(empty)
                {
                    Manager.instance.playerCanAttack = false;
                }
                else
                {
                    Manager.instance.playerCanAttack = true;
                }
            }
        }

        public void addItem(item item)
        {
            currentItem = item;
            empty = false;
            currentItemSprite = Instantiate(inventoryInstance.itemSprite, transform.position, Quaternion.identity, transform);
            currentItemSprite.GetComponent<Image>().sprite = currentItem.itemIcon;
        }
        public void removeItem()
        {
            currentItem = null;
            empty = true;
            Destroy(currentItemSprite);
        }
        public void mouseEnter() 
        {
            inventoryInstance.mouseOverSlot = this;
            if(!empty)
            {
                inventoryInstance.ODescription = currentItem.itemDescription;
            }
            else if(inventoryInstance.dragingItem)
            {
                inventoryInstance.ODescription = inventoryInstance.dragFrom.currentItem.itemDescription;
            }
            if(!empty && !craftslot && !gunSlot && !meleeSlot && !usingItem)
            {
                XBtn.SetActive(true);
            }
        }
        public void mouseExit() 
        {
            inventoryInstance.mouseOverSlot = null;
            XBtn.SetActive(false);
        }
        public void removingFromSlot()
        {
            inventoryInstance.removingItem = slotIndex;
        }
        private void Update() 
        {
            if(currentItemSprite != null)
            {
                if(usingItem)
                {
                    currentItemSprite.GetComponent<Image>().color = new Color(1, 1, 1, 0.3f);
                }
                else
                {
                    currentItemSprite.GetComponent<Image>().color = new Color(1, 1, 1, 1);
                }
            }
            if(gunSlot)
            {
                if(empty)
                {
                    Manager.instance.playerCanShot = false;
                }
                else
                {
                    Manager.instance.playerCanShot = true;
                }
            }
            if(meleeSlot)
            {
                if(empty)
                {
                    Manager.instance.playerCanAttack = false;
                }
                else
                {
                    Manager.instance.playerCanAttack = true;
                }
            }
            /*if(gunSlot)
            {
                if(!empty)
                {
                    GameObject.FindGameObjectWithTag("Player").GetComponent<fighting>().gun = currentItem as equipment;
                }
                else
                {
                    GameObject.FindGameObjectWithTag("Player").GetComponent<fighting>().gun = null;
                }
            }
            if(meleeSlot)
            {
                if(!empty)
                {
                    GameObject.FindGameObjectWithTag("Player").GetComponent<fighting>().melee = currentItem as equipment;
                }
                else
                {
                    GameObject.FindGameObjectWithTag("Player").GetComponent<fighting>().melee = null;
                }
            }*/
        }
    }
}
