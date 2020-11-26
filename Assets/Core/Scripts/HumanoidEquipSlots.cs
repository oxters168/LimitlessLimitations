using UnityEngine;

public class HumanoidEquipSlots : MonoBehaviour
{
    public enum SlotSpace { head, rightHand, leftHand, body, }
    private ItemData[] bodySlots = new ItemData[4];

    public void SetItem(ItemData item, SlotSpace slot)
    {
        bodySlots[(int)slot] = item;
    }
    public ItemData GetItem(SlotSpace slot)
    {
        return bodySlots[(int)slot];
    }
}
