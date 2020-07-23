using System.Collections.Generic;
using UnityEngine.UI;
using FGJ.Interaction;
using TMPro;
using UnityEngine;

namespace FGJ.UI
{
    public class inventory : MonoBehaviour
    {
        [HideInInspector] public static inventory inventoryInstance;
        [SerializeField] GameObject itemName;
        [SerializeField] TMP_Text itemDescription;
        [HideInInspector] public int removingItem;
        [HideInInspector] public bool dragingItem;
        [HideInInspector] public string ODescription;
        public GameObject itemSprite;
        private GameObject dragingItemSprite;
        [HideInInspector] public inventorySlot dragFrom;
        public List<item> inventoryList;
        [HideInInspector] public inventorySlot mouseOverSlot;
        private inventorySlot[] InventorySlots;
        private void Awake() 
        {
            inventoryInstance = this;
        }
        private void Start() 
        {   
            InventorySlots = GetComponentsInChildren<inventorySlot>();
            for(int i = 0; i < InventorySlots.Length; i++)
            {
                InventorySlots[i].slotIndex = i;
            }
            transform.parent.gameObject.SetActive(false);
        }
        public bool addItem(item newItem, int slotIndex)
        {
            if(inventoryList.Count < InventorySlots.Length)
            {
                if(slotIndex >= 0)
                {
                    if(InventorySlots[slotIndex].empty)
                    {
                        InventorySlots[slotIndex].addItem(newItem);
                        inventoryList.Add(newItem);
                        return true;
                    }
                    return false;
                }
                else
                {
                    for(int i = 0; i < InventorySlots.Length; i++)
                    {
                        if(InventorySlots[i].empty)
                        {
                            InventorySlots[i].addItem(newItem);
                            inventoryList.Add(newItem);
                            return true;
                        }
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public void removeItem()
        {
            if(!InventorySlots[removingItem].empty)
            {
                inventoryList.Remove(InventorySlots[removingItem].currentItem);
                InventorySlots[removingItem].removeItem();
            }
        }
        public void dropItem()
        {
            if(!InventorySlots[removingItem].empty)
            {
                if(InventorySlots[removingItem].currentItem.itemObject != null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    InventorySlots[removingItem].currentItem.itemObject.GetComponent<pickUp>().O_Item = InventorySlots[removingItem].currentItem;
                    Instantiate(InventorySlots[removingItem].currentItem.itemObject, player.transform.position + player.transform.forward * 2f, Quaternion.identity);
                }
                inventoryList.Remove(InventorySlots[removingItem].currentItem);
                InventorySlots[removingItem].removeItem();
            }
        }
        public void clearInventory()
        {
            for(int i = 0; i < inventoryList.Count; i ++)
            {
                removingItem = i;
                removeItem();
            }
        }
        public void mouseDrop()
        {
            if(dragFrom != null)
            {
                dragFrom.usingItem = false;
            }
            if((mouseOverSlot != null && mouseOverSlot.empty && dragFrom != null && !mouseOverSlot.resultSlot && !mouseOverSlot.gunSlot && !mouseOverSlot.meleeSlot) || (mouseOverSlot != null && dragFrom != null && mouseOverSlot == dragFrom.cameFrom) || (mouseOverSlot != null && dragFrom != null && ((mouseOverSlot.gunSlot && dragFrom.currentItem.itemType == item.EItemType.gun) || (mouseOverSlot.meleeSlot && dragFrom.currentItem.itemType == item.EItemType.melee))))
            {
                if(mouseOverSlot == dragFrom.cameFrom)
                {
                    mouseOverSlot.removeItem();
                    mouseOverSlot.usingItem = false;
                }
                if(dragFrom.resultSlot)
                {
                    inventoryList.Add(dragFrom.currentItem);
                    inventoryList.Remove(dragFrom.GetComponent<crafting>().comp1.currentItem);
                    dragFrom.GetComponent<crafting>().comp1.removeItem();
                    dragFrom.GetComponent<crafting>().comp1.cameFrom.usingItem = false;
                    dragFrom.GetComponent<crafting>().comp1.cameFrom.removeItem();
                    inventoryList.Remove(dragFrom.GetComponent<crafting>().comp2.currentItem);
                    dragFrom.GetComponent<crafting>().comp2.removeItem();
                    dragFrom.GetComponent<crafting>().comp2.cameFrom.usingItem = false;
                    dragFrom.GetComponent<crafting>().comp2.cameFrom.removeItem();
                }
                if((dragFrom.craftslot && !dragFrom.resultSlot) || dragFrom.gunSlot || dragFrom.meleeSlot)
                {
                    if((mouseOverSlot == null || mouseOverSlot != dragFrom.cameFrom) && !mouseOverSlot.craftslot)
                    {
                        dragFrom.cameFrom.usingItem = false;
                        dragFrom.cameFrom.removeItem();
                    }
                }
                mouseOverSlot.addItem(dragFrom.currentItem);
                if((!mouseOverSlot.craftslot && !mouseOverSlot.gunSlot && !mouseOverSlot.meleeSlot) || (mouseOverSlot.craftslot && dragFrom.craftslot))
                {
                   dragFrom.removeItem();
                }
                else
                {
                    dragFrom.usingItem = true;
                    dragFrom.usingIn = mouseOverSlot;
                }
                if(!dragFrom.craftslot)
                {
                   mouseOverSlot.cameFrom = dragFrom; 
                }
                else
                {
                    mouseOverSlot.cameFrom = dragFrom.cameFrom;
                }
            }
            dragingItem = false;
            dragFrom = null;
            Destroy(dragingItemSprite);
        }
        public void mouseGet()
        {
            if(mouseOverSlot != null && !mouseOverSlot.empty && !mouseOverSlot.usingItem)
            {
                dragingItemSprite = Instantiate(itemSprite, Input.mousePosition, Quaternion.identity, transform);
                dragingItemSprite.GetComponent<Image>().sprite = mouseOverSlot.currentItem.itemIcon;
                dragFrom = mouseOverSlot;
                dragFrom.usingItem = true;
                dragFrom.usingIn = null;
                dragingItem = true;
            }
        }
        public void clearUsedItems()
        {
            inventorySlot[] slots = gameObject.transform.parent.GetComponentsInChildren<inventorySlot>();
            for(int i = 0; i < slots.Length; i++)
            {
                if(slots[i].craftslot)
                {
                    slots[i].removeItem();
                }
                if((slots[i].usingItem && slots[i].usingIn == null) || (slots[i].usingItem && !slots[i].usingIn.gunSlot && !slots[i].usingIn.meleeSlot))
                {
                    slots[i].usingItem = false;
                }
            }
        }
        private void Update() 
        {
            if(dragingItem)
            {
                dragingItemSprite.transform.position = Input.mousePosition;
            }
            if(Input.GetMouseButtonDown(0))
            {
                mouseGet();
            }
            if(Input.GetMouseButtonUp(0))
            {
                mouseDrop();
            }
            if(mouseOverSlot != null && mouseOverSlot.currentItem != null)
            {
                Vector3 pannelPosition = new Vector3(Input.mousePosition.x - (itemName.GetComponent<RectTransform>().rect.x), Input.mousePosition.y + (itemName.GetComponent<RectTransform>().rect.y), 0);
                if(Input.mousePosition.x - ((itemName.GetComponent<RectTransform>().rect.x) * 2) > Screen.width)
                {
                    pannelPosition = new Vector3(Input.mousePosition.x + (itemName.GetComponent<RectTransform>().rect.x), pannelPosition.y, 0);
                }
                if(Input.mousePosition.y + ((itemName.GetComponent<RectTransform>().rect.y) * 2) < 0f)
                {
                    pannelPosition = new Vector3(pannelPosition.x, Input.mousePosition.y - (itemName.GetComponent<RectTransform>().rect.y), 0);
                }
                itemName.transform.position = pannelPosition;
                itemName.gameObject.SetActive(true);
                itemName.GetComponentInChildren<TMP_Text>().text = mouseOverSlot.currentItem.itemName;
                itemDescription.text = mouseOverSlot.currentItem.itemDescription;
                if(mouseOverSlot.currentItem.itemType == item.EItemType.melee)
                {
                    itemDescription.text = ODescription + "\nDano: " + (mouseOverSlot.currentItem as equipment).damage + "\nVelocidade: " + (mouseOverSlot.currentItem as equipment).speed;
                }
                if(mouseOverSlot.currentItem.itemType == item.EItemType.gun)
                {
                    itemDescription.text = ODescription + "\nDano: " + (mouseOverSlot.currentItem as equipment).damage + "\nCadência: " + (mouseOverSlot.currentItem as equipment).rateOfFire+ "\nPrecisão " + (mouseOverSlot.currentItem as equipment).rateOfFire;
                }
                if(mouseOverSlot.currentItem as craftMaterial != null)
                {
                    itemDescription.text = ODescription + "\n + " + (mouseOverSlot.currentItem as craftMaterial).iDamage + " Dano\n + " + (mouseOverSlot.currentItem as craftMaterial).iSpeed + " Velocidade\n + " + (mouseOverSlot.currentItem as craftMaterial).iRateOfFire + " Cadência\n + " + (mouseOverSlot.currentItem as craftMaterial).iAcurracy + " Precisão";
                }
            }
            else
            {
                itemName.gameObject.SetActive(false);
            }
        }
    }
}
