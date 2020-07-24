using UnityEngine.SceneManagement;
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
        [SerializeField] bool mainMenu;
        public GameObject inventoryUI;
        public GameObject pauseMenu;
        [HideInInspector] public bool inventaryOpen;
        [HideInInspector] public bool pauseMenuOpen;
        private void Awake() 
        {
            instance = this;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if(mainMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
        public void pauseGame()
        {
            pauseMenu.SetActive(true);
            playerCanMove = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pauseMenuOpen = true;
            Time.timeScale = 0f;
        }
        public void resumeGame()
        {
            pauseMenu.SetActive(false);
            playerCanMove = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            pauseMenuOpen = false;
            Time.timeScale = 1f;
        }
        public void startGame()
        {
            SceneManager.LoadScene("Game Jam Test Scene");
        }
        public void loadMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
        public void quitGame()
        {
            Application.Quit();
        }
    }
}
