using System.Collections.Generic;
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
        [SerializeField] List<AudioClip> ambientMusics;
        [SerializeField] AudioClip creditsMusic;
        [SerializeField] AudioClip combatMusic;
        [SerializeField] bool mainMenu;
        [SerializeField] bool autoPlay = true;
        public GameObject inventoryUI;
        public GameObject dialogUI;
        public GameObject pauseMenu;
        [HideInInspector] public bool inventaryOpen;
        [HideInInspector] public bool pauseMenuOpen;
        [SerializeField] float timeBetweenMusics;
        private int currentMusic;
        private float musicTimer;
        private float combatTimer;
        private AudioSource audioSrc;
        public bool playerInCombat;

        private void Awake() 
        {
            instance = this;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
            if(mainMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            audioSrc = GetComponent<AudioSource>();
        }
        private void Start() 
        {
            if(dialogUI != null)
            {
                dialogUI.SetActive(true);
                dialogUI.GetComponent<dialogUI>().startDialog(0, 5);
            }
            musicTimer = timeBetweenMusics + 1;
        }
        private void OnEnable() 
        {
            playerCanAttack = false;
            playerCanShot = false;
        }
        public void openInventory()
        {
            if(!dialogUI.GetComponent<dialogUI>().inDialog)
            {
                inventoryUI.SetActive(true);
                playerCanMove = false;
                inventaryOpen = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
            if(!dialogUI.GetComponent<dialogUI>().inDialog)
            {
                pauseMenu.SetActive(true);
                playerCanMove = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                pauseMenuOpen = true;
                Time.timeScale = 0f;
            }
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
        private void Update() 
        {
            if(autoPlay)
            {
                if(ambientMusics.Count > 0)
                {
                    if(ambientMusics.Count == 1)
                    {
                        currentMusic = 0;
                    }
                    if((!audioSrc.isPlaying || audioSrc.clip != ambientMusics[currentMusic]) && musicTimer > timeBetweenMusics)
                    {
                        audioSrc.clip = ambientMusics[currentMusic];
                        audioSrc.Play();
                        musicTimer = 0f;
                        currentMusic++;
                    }
                    else
                    {
                        if(!audioSrc.isPlaying)
                        {
                            musicTimer += Time.deltaTime;
                        }
                    }
                    if(currentMusic == ambientMusics.Count)
                    {
                        currentMusic = 0;
                    }
                }
            }
            if(!mainMenu)
            {
                combatTimer += Time.deltaTime;
            }
            if(!playerInCombat && combatTimer > 1)
            {
                if(!autoPlay)
                {
                    endCombat();
                }
            }
        }
        public void setAutoPlay(bool AutoPlay)
        {
            autoPlay = AutoPlay;
            musicTimer = timeBetweenMusics + 1;
        }
        public void playMusic()
        {
            if(mainMenu)
            {
                audioSrc.clip = creditsMusic;
                audioSrc.Play();
            }
            else
            {
                audioSrc.clip = combatMusic;
                audioSrc.Play();
            }
        }
        public void setCombat(bool inCombat)
        {
            if(inCombat)
            {
                playerInCombat = true;
                autoPlay = false;
                if(audioSrc.clip != combatMusic)
                {
                    playMusic();
                }
            }
            if(!inCombat && playerInCombat)
            {
                combatTimer = 0f;
                playerInCombat = false;
            }
        }
        private void endCombat()
        {
            musicTimer = timeBetweenMusics + 1;
            autoPlay = true;
        }
    }
}
