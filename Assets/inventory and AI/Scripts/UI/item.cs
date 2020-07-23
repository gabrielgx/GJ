using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class item : ScriptableObject
{
    public enum EItemType {material, gun, melee}

    public string itemName = "ItemName";
    [TextArea] public string itemDescription = "description";
    public Sprite itemIcon = null;
    public EItemType itemType;
    //public string itemType = "default";
    public GameObject itemObject = null;
}
