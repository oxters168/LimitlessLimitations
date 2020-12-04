using UnityEngine;
using System.Collections.Generic;

public class ItemInventory : MonoBehaviour
{
    private List<ItemData> items = new List<ItemData>();

    public ItemData[] GetItems()
    {
        return items.ToArray();
    }
}
