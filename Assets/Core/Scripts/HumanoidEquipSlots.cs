using UnityEngine;

public class HumanoidEquipSlots : MonoBehaviour
{
    public ItemWildcard leftHandWildcard, rightHandWildcard, leftShoulderWildcard, rightShoulderWildcard, leftHipWildcard, rightHipWildcard;

    public bool isHeld;
    private bool prevIsHeld;

    public enum SlotSpace { head, leftHand, rightHand, body, }
    private ItemData[] bodySlots = new ItemData[4];

    void Update()
    {
        if (isHeld && !prevIsHeld)
        {
            prevIsHeld = isHeld;
            RefreshSlots();
        }
    }
    public void SetItem(ItemData item, SlotSpace slot)
    {
        bodySlots[(int)slot] = item;
        RefreshSlots();
    }
    public ItemData GetItem(SlotSpace slot)
    {
        return bodySlots[(int)slot];
    }
    private void RefreshSlots()
    {
        SetSlots(bodySlots[(int)SlotSpace.leftHand], leftShoulderWildcard, leftHipWildcard, leftHandWildcard, isHeld);
        SetSlots(bodySlots[(int)SlotSpace.rightHand], rightShoulderWildcard, rightHipWildcard, rightHandWildcard, isHeld);
    }
    private static void SetSlots(ItemData item, ItemWildcard shoulder, ItemWildcard hip, ItemWildcard hand, bool isHeld)
    {
        if (item != null && item is WeaponData)
        {
            WeaponData weaponItem = (WeaponData)item;
            shoulder.currentItem = (!isHeld && weaponItem.type != WeaponType.dagger) ? weaponItem : null;
            hip.currentItem = (!isHeld && weaponItem.type == WeaponType.dagger) ? weaponItem : null;
            hand.currentItem = isHeld ? weaponItem : null;
        }
        else
        {
            shoulder.currentItem = null;
            hip.currentItem = null;
            hand.currentItem = null;
        }
    }
}
