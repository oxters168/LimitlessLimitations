using UnityEngine;
using UnityHelpers;

public class HumanoidCustomMoves : MonoBehaviour
{
    private const int DUAL_SHOULDER_TYPE = -1;
    private const int DUAL_HIP_TYPE = -2;

    private AnimateAndMoveCharacter _character;
    private AnimateAndMoveCharacter Character { get { if (_character == null) _character = GetComponent<AnimateAndMoveCharacter>(); return _character; } }
    private HumanoidEquipSlots _equipSlots;
    private HumanoidEquipSlots EquipSlots { get { if (_equipSlots == null) _equipSlots = GetComponent<HumanoidEquipSlots>(); return _equipSlots; } }

    private Animator _animator;
    private Animator animator { get { if (_animator == null) _animator = GetComponent<Animator>(); return _animator; } }

    [Tooltip("How fast the character moves when strafing (in meters per second)")]
    public float strafeSpeed = 3.2f;
    private bool isStrafing;

    public CustomAnimationData rollData;
    public CustomAnimationData attackData;
    public CustomAnimationData unsheathData;

    private bool prevRoll;
    private bool prevAttack;

    private CustomAnimationData currentCustomAnim;
    private float customAnimStartTime;
    public bool IsRunningCustomAnim { get { return currentCustomAnim != null && CustomAnimPercentPlayed <= 1; } }
    public float CustomAnimPercentPlayed { get { return currentCustomAnim != null ? (Time.time - customAnimStartTime) / currentCustomAnim.executionTime : float.MinValue; } }
    private Vector2 customAnimStartDir;
    private Vector2 customAnimStartPos;
    private bool toggledCustomAnim;

    [Space(10)]
    public WeaponData testWeaponL;
    private WeaponData prevWeaponL;
    public WeaponData testWeaponR;
    private WeaponData prevWeaponR;

    void Update()
    {
        if (prevWeaponL != testWeaponL)
        {
            prevWeaponL = testWeaponL;
            EquipSlots.SetItem(testWeaponL, HumanoidEquipSlots.SlotSpace.leftHand);
        }
        if (prevWeaponR != testWeaponR)
        {
            prevWeaponR = testWeaponR;
            EquipSlots.SetItem(testWeaponR, HumanoidEquipSlots.SlotSpace.rightHand);
        }

        bool leftHanded;
        int weaponType = GetWeaponType(EquipSlots.GetItem(HumanoidEquipSlots.SlotSpace.leftHand), EquipSlots.GetItem(HumanoidEquipSlots.SlotSpace.rightHand), out leftHanded);

        //Input stuff
        Vector2 input = new Vector2(Character.GetAxis("dpadHor"), Character.GetAxis("dpadVer"));
        isStrafing = Character.GetToggle("l2Btn") && !Character.IsUnderwater;
        if ((!IsRunningCustomAnim || currentCustomAnim.canBeInterrupted) && !Character.IsUnderwater)
        {
            CustomAnimationData nextAnimData = null;
            Vector2 direction = transform.forward.xz().normalized;

            if (Character.GetToggle("circleBtn"))
            {
                if (!prevRoll)
                {
                    prevRoll = true;
                    nextAnimData = rollData;
                    if (isStrafing && !input.IsZero())
                        direction = input.normalized;
                }
            }
            else
                prevRoll = false;

            if (Character.GetToggle("squareBtn"))
            {
                if (!prevAttack && weaponType > 0)
                {
                    prevAttack = true;
                    if (EquipSlots.isHeld)
                        nextAnimData = attackData;
                    else
                        nextAnimData = unsheathData;
                        // nextAnimData = GetUnsheathAnimData(sheathType);
                }
            }
            else
                prevAttack = false;

            // if (nextAnimData != null && (!IsRunningCustomAnim || currentCustomAnim != nextAnimData))
            if (nextAnimData != null && (!IsRunningCustomAnim || (currentCustomAnim.canBeInterrupted && currentCustomAnim != nextAnimData)))
                SetCustomAnim(nextAnimData, direction);
        }

        animator.SetFloat("StrafeDir", PercentClockwise(transform.forward.xz().normalized, input.normalized));
        animator.SetInteger("WeaponType", weaponType);
        // animator.SetInteger("SheathType", (int)sheathType);
        animator.SetBool("LeftHanded", leftHanded);

        //Custom anim stuff
        animator.SetLayerWeight(animator.GetLayerIndex("Upperbody"), (IsRunningCustomAnim && currentCustomAnim.isUpperbody) ? 1 : 0);
        animator.SetBool("CustomAnim", IsRunningCustomAnim && currentCustomAnim.blockAnimation);
        Character.blockMovement = (IsRunningCustomAnim && currentCustomAnim.blockMovement) || isStrafing;
        Character.blockRotation = (IsRunningCustomAnim && currentCustomAnim.blockRotation) || isStrafing;
        if (IsRunningCustomAnim)
        {
            if (!toggledCustomAnim)
            {
                animator.SetTrigger(currentCustomAnim.animationToggleName);
                toggledCustomAnim = true;
            }

            if (currentCustomAnim.distanceTravelled > float.Epsilon)
            {
                Vector2 nextPosition = customAnimStartPos + customAnimStartDir * currentCustomAnim.distanceTravelled * currentCustomAnim.travelMap.Evaluate(CustomAnimPercentPlayed);
                Character.SetPosition(nextPosition);
            }
        }
        else
            toggledCustomAnim = false;

        //Strafe stuff
        animator.SetBool("Strafe", isStrafing && !input.IsZero());
        if (isStrafing && (!IsRunningCustomAnim || (IsRunningCustomAnim && !currentCustomAnim.blockMovement)))
        {            
            Vector2 nextPosition = transform.position.xz() + input.normalized * strafeSpeed * Time.deltaTime;
            Character.SetPosition(nextPosition);
        }
    }

    private float PercentClockwise(Vector2 first, Vector2 second)
    {
        return first.GetClockwiseAngle(second) / 360;
    }
    private void SetCustomAnim(CustomAnimationData customAnim)
    {
        SetCustomAnim(customAnim, transform.forward.xz().normalized);
    }
    private void SetCustomAnim(CustomAnimationData customAnim, Vector2 dir)
    {
        // if (!IsRunningCustomAnim || (currentCustomAnim.canBeInterrupted && currentCustomAnim != customAnim))
            toggledCustomAnim = false;

        currentCustomAnim = customAnim;
        customAnimStartTime = Time.time;
        customAnimStartDir = dir;
        customAnimStartPos = transform.position.xz();
    }
    private static int GetWeaponType(ItemData leftHandItem, ItemData rightHandItem, out bool leftHanded)
    {
        WeaponData leftHandWeapon = null;
        WeaponData rightHandWeapon = null;
        if (leftHandItem is WeaponData)
            leftHandWeapon = (WeaponData)leftHandItem;
        if (rightHandItem is WeaponData)
            rightHandWeapon = (WeaponData)rightHandItem;

        leftHanded = false;

        int weaponType = -1;
        if (rightHandWeapon != null && leftHandWeapon != null)
        {
            if (rightHandWeapon.type == WeaponType.shield && leftHandWeapon.type != WeaponType.shield)
            {
                leftHanded = true;
                weaponType = (int)leftHandWeapon.type;
            }
            else if (leftHandWeapon.type == WeaponType.shield && rightHandWeapon.type != WeaponType.shield)
            {
                weaponType = (int)rightHandWeapon.type;
            }
            else
            {
                if (rightHandWeapon.type == WeaponType.dagger && leftHandWeapon.type == WeaponType.dagger)
                    weaponType = DUAL_HIP_TYPE;
                else
                    weaponType = DUAL_SHOULDER_TYPE;
            }
        }
        else if (rightHandWeapon != null)
        {
            weaponType = (int)rightHandWeapon.type;
        }
        else if (leftHandWeapon != null)
        {
            leftHanded = true;
            weaponType = (int)leftHandWeapon.type;
        }

        return weaponType;
    }

    public void Hit()
    {
        // Debug.Log("Hit!");
    }
    public void FootL()
    {
    }
    public void FootR()
    {
    }
    public void WeaponSwitch()
    {
        EquipSlots.isHeld = !EquipSlots.isHeld;
    }
}
