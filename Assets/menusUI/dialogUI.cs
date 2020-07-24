using System.Collections;
using System.Collections.Generic;
using FGJ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class dialogUI : MonoBehaviour
{
    public List<dialogue> dialogs;
    public GameObject sImage;
    public GameObject background;
    public TMP_Text nameText;
    public TMP_Text dialogText;
    public bool inDialog;
    int firstDialog;
    int lastDialog;
    bool changedDialog;
    int i;

    public void startDialog(int start, int finish)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Manager.instance.playerCanMove = false;
        gameObject.SetActive(true);
        lastDialog = finish;
        firstDialog = start;
        inDialog = true;
        changedDialog = false;
        i = start;
    }
    
    void Update() 
    {
        if(inDialog)
        {
            if(!changedDialog)
            {
                nameText.text = dialogs[i].speaker;
                dialogText.text = dialogs[i].speech;
                if(dialogs[i].speakerImage != null)
                {
                    Vector2 imageSize = sImage.GetComponent<RectTransform>().sizeDelta;
                    Vector2 spriteSize = new Vector2(dialogs[i].speakerImage.bounds.size.x, dialogs[i].speakerImage.bounds.size.y);
                    sImage.SetActive(true);
                    sImage.GetComponent<Image>().sprite = dialogs[i].speakerImage;
                    sImage.GetComponent<RectTransform>().sizeDelta = new Vector2((spriteSize.x/spriteSize.y) * imageSize.y, imageSize.y);
                }
                else
                {
                    sImage.SetActive(false);
                    sImage.GetComponent<Image>().sprite = null;
                }
                if(dialogs[i].sound != null)
                {
                    GetComponentInChildren<AudioSource>().clip = dialogs[i].sound;
                    GetComponentInChildren<AudioSource>().Play();
                }
                if(dialogs[i].useBG)
                {
                    if(dialogs[i].background != null)
                    {
                        background.GetComponent<Image>().sprite = dialogs[i].background;
                        background.GetComponent<Image>().color = Color.white;
                    }
                    else
                    {
                        background.GetComponent<Image>().color = Color.black;
                    }
                    background.SetActive(true);
                }
                else
                {
                    background.SetActive(false);
                }
                changedDialog = true;
            }
            if(Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.E))
            {
                changedDialog = false;
                i++;
            }
            if(i > lastDialog)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                Manager.instance.playerCanMove = true;
                inDialog = false;
                gameObject.SetActive(false);
            }
        }
    }
}
