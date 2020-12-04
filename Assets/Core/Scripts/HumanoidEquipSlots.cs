using UnityEngine;
using UnityHelpers;

public class HumanoidEquipSlots : MonoBehaviour
{
    public ItemWildcard leftHandWildcard, rightHandWildcard, leftShoulderWildcard, rightShoulderWildcard, leftHipWildcard, rightHipWildcard;

    public bool isHeld;
    private bool prevIsHeld;

    public enum SlotSpace { head, leftHand, rightHand, body, }
    private ItemData[] bodySlots = new ItemData[4];

    void Update()
    {
        RefreshAnchors();

        if (isHeld != prevIsHeld)
        {
            prevIsHeld = isHeld;
            RefreshSlots();
        }
    }

    public void SetVisible(bool isOn)
    {
        leftHandWildcard.gameObject.SetActive(isOn);
        rightHandWildcard.gameObject.SetActive(isOn);
        leftShoulderWildcard.gameObject.SetActive(isOn);
        rightShoulderWildcard.gameObject.SetActive(isOn);
        leftHipWildcard.gameObject.SetActive(isOn);
        rightHipWildcard.gameObject.SetActive(isOn);
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

    private void RefreshAnchors()
    {
        var wildcards = new ItemWildcard[] { leftHandWildcard, rightHandWildcard, leftShoulderWildcard, rightShoulderWildcard, leftHipWildcard, rightHipWildcard };
        HumanoidShell currentShell = null;
        for (int i = 0; i < wildcards.Length; i++)
        {
            var currentMimic = wildcards[i].GetComponent<MimicTransform>();
            if (currentMimic.other == null || !currentMimic.other.gameObject.activeInHierarchy)
            {
                if (currentShell == null)
                    currentShell = GetComponentInChildren<HumanoidShell>();

                string anchorName = currentMimic.name;
                anchorName = anchorName.Replace("Wildcard", "Anchor");
                currentMimic.other = currentShell.GetAnchor(anchorName);
            }
        }
    }
}
