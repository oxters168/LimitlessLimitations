using UnityEngine;

public class ItemWildcard : MonoBehaviour
{
    public ItemData currentItem;
    private ItemData prevItem;

    void Start()
    {
        RefreshShownItem();
    }
    void Update()
    {
        if (currentItem != prevItem)
            RefreshShownItem();
    }
    
    public void RefreshShownItem()
    {
        var items = GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            bool isCurrentItem = currentItem != null && items[i].data.name.Equals(currentItem.name);
            items[i].gameObject.SetActive(isCurrentItem);
        }
        prevItem = currentItem;
    }
    public Item[] GetItems()
    {
        return GetComponentsInChildren<Item>(true);
    }
}
