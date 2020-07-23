using System.Collections;
using System.Collections.Generic;
using FGJ.Interaction;
using UnityEngine;

namespace FGJ.UI
{
    public class crafting : MonoBehaviour
    {
        public inventorySlot comp1, comp2;
        [SerializeField] item testItem1, testItem2, testItem3;
        public List<item> craftComps;
        private equipment resultEquip = null;

        void Update() 
        {
            craftComps[0] = comp1.currentItem;
            craftComps[1] = comp2.currentItem;
            if(!comp1.empty && !comp2.empty)
            {
                if(GetComponent<inventorySlot>().empty)
                {
                    if(craftComps.Contains(testItem1) && craftComps.Contains(testItem2))
                    {
                        GetComponent<inventorySlot>().addItem(testItem3);
                    }
                    else if(craftComps[0] != null && craftComps[0] as equipment != null && craftComps[1] != null && craftComps[1] as craftMaterial != null)
                    {
                        resultEquip = ScriptableObject.CreateInstance("equipment") as equipment;
                        resultEquip.itemName = craftComps[0].itemName + "+";
                        resultEquip.itemDescription = craftComps[0].itemDescription;
                        resultEquip.itemIcon = craftComps[0].itemIcon;
                        resultEquip.itemType = craftComps[0].itemType;
                        resultEquip.itemObject = craftComps[0].itemObject;
                        resultEquip.damage = (craftComps[0] as equipment).damage + (craftComps[1] as craftMaterial).iDamage;
                        resultEquip.speed = (craftComps[0] as equipment).speed + (craftComps[1] as craftMaterial).iSpeed; 
                        resultEquip.rateOfFire = (craftComps[0] as equipment).rateOfFire + (craftComps[1] as craftMaterial).iRateOfFire; 
                        resultEquip.acurracy = (craftComps[0] as equipment).acurracy + (craftComps[1] as craftMaterial).iAcurracy;
                        GetComponent<inventorySlot>().addItem(resultEquip);
                    }
                    else if(craftComps[1] != null && craftComps[1] as equipment != null && craftComps[0] != null && craftComps[0].itemType == item.EItemType.material)
                    {
                        resultEquip = ScriptableObject.CreateInstance("equipment") as equipment;
                        resultEquip.itemName = craftComps[1].itemName + "+";
                        resultEquip.itemDescription = craftComps[1].itemDescription;
                        resultEquip.itemIcon = craftComps[1].itemIcon;
                        resultEquip.itemType = craftComps[1].itemType;
                        resultEquip.itemObject = craftComps[1].itemObject;
                        resultEquip.damage = (craftComps[1] as equipment).damage + (craftComps[0] as craftMaterial).iDamage;
                        resultEquip.speed = (craftComps[1] as equipment).speed + (craftComps[0] as craftMaterial).iSpeed; 
                        resultEquip.rateOfFire = (craftComps[1] as equipment).rateOfFire + (craftComps[0] as craftMaterial).iRateOfFire; 
                        resultEquip.acurracy = (craftComps[1] as equipment).acurracy + (craftComps[0] as craftMaterial).iAcurracy;
                        GetComponent<inventorySlot>().addItem(resultEquip);
                    }
                }
            }
            else
            {
                if(!GetComponent<inventorySlot>().empty)
                {
                    GetComponent<inventorySlot>().removeItem();
                }
            }
        }
    }
}