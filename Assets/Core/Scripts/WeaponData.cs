using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "WeaponData", order = 51)]
public class WeaponData : ItemData
{
    [Tooltip("This determines the type of the weapon")]
    public WeaponType type;
    [Tooltip("This affects how much damage is dealt to another entity by this weapon")]
    public float strength;
    [Range(0, 1), Tooltip("This determines how much damage from an attack the holder takes when blocking with this weapon")]
    public float blockPercent;
}
