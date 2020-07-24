using UnityEngine;

[CreateAssetMenu(fileName = "New dialogue", menuName = "UI/Dialogue")]
public class dialogue : ScriptableObject 
{
    public bool useBG;
    public Sprite background = null;
    public Sprite speakerImage = null;
    public string speaker;
    public string speech;
    public AudioClip sound = null;
}