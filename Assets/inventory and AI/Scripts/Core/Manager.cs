using System.Collections;
using System.Collections.Generic;
using FGJ.UI;
using UnityEngine;

namespace FGJ.Core
{
    public class Manager : MonoBehaviour
    {
        [HideInInspector] public static Manager instance;
        [HideInInspector] public bool playerCanMove = true;
        [HideInInspector] public bool playerCanAttack = true;
        [HideInInspector] public bool playerCanShot = true;
        public GameObject inventoryUI;
        [HideInInspector] public bool inventaryOpen;
        private void Awake() 
        {
            instance = this;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        public void openInventory()
        {
            inventoryUI.SetActive(true);
            playerCanMove = false;
            inventaryOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        public void closeInventory()
        {
            inventoryUI.GetComponentInChildren<inventory>().clearUsedItems();
            inventoryUI.GetComponentInChildren<inventory>().mouseOverSlot = null;
            if(inventoryUI.GetComponentInChildren<inventory>().dragingItem)
            {
                inventoryUI.GetComponentInChildren<inventory>().mouseDrop();
            }
            inventoryUI.SetActive(false);
            playerCanMove = true;
            inventaryOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
