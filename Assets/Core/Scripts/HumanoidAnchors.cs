using UnityEngine;

public class HumanoidAnchors : MonoBehaviour
{
    public Transform leftHandAnchor, rightHandAnchor, leftShoulderAnchor, rightShoulderAnchor, leftHipAnchor, rightHipAnchor;

    private HumanoidCustomMoves _combatLink;
    private HumanoidCustomMoves CombatLink { get { if (_combatLink == null) _combatLink = GetComponentInParent<HumanoidCustomMoves>(); return _combatLink; } }

    public Transform GetAnchor(string name)
    {
        var anchors = new Transform[] { leftHandAnchor, rightHandAnchor, leftShoulderAnchor, rightShoulderAnchor, leftHipAnchor, rightHipAnchor };
        Transform anchor = null;
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i].name.Equals(name))
            {
                anchor = anchors[i];
                break;
            }
        }
        return anchor;
    }
    
    public void Hit()
    {
        CombatLink.Hit();
    }
    public void FootL()
    {
        CombatLink.FootL();
    }
    public void FootR()
    {
        CombatLink.FootR();
    }
    public void WeaponSwitch()
    {
        CombatLink.WeaponSwitch();
    }
    public void Shoot()
    {
        CombatLink.Shoot();
    }
}
