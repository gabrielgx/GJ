using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Equipment")]
public class equipment : item
{
    public float damage;
    public float speed;
    public float rateOfFire;
    public float acurracy;
}
