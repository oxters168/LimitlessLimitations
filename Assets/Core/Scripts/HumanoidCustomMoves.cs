using UnityEngine;
using UnityHelpers;

public class HumanoidCustomMoves : MonoBehaviour
{
    private const int DUAL_SHOULDER_TYPE = -1;
    private const int DUAL_HIP_TYPE = -2;
    private const int WEAPON_NONE = int.MinValue;

    private AnimateAndMoveCharacter _character;
    private AnimateAndMoveCharacter Character { get { if (_character == null) _character = GetComponent<AnimateAndMoveCharacter>(); return _character; } }
    private HumanoidEquipSlots _equipSlots;
    private HumanoidEquipSlots EquipSlots { get { if (_equipSlots == null) _equipSlots = GetComponent<HumanoidEquipSlots>(); return _equipSlots; } }

    private Animator _animator;
    private Animator animator { get { if (_animator == null || !_animator.gameObject.activeInHierarchy) _animator = GetComponentInChildren<Animator>(); return _animator; } }

    [Tooltip("How fast the character moves when strafing (in meters per second)")]
    public float strafeSpeed = 3.2f;
    private bool isStrafing;
    private bool isBlocking;

    public CustomAnimationData rollData;
    public CustomAnimationData attackData;
    public CustomAnimationData unsheathData;
    public CustomAnimationData sheathData;

    [Space(10), Tooltip("How long before the combo breaks")]
    public float attackComboTime = 0.2f;
    private float currentAttackComboTime;
    private int attackCombo;
    private float lastAttack;

    private bool prevRoll;
    private bool prevAttack;
    private bool prevSheath;

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

        if ((Character.IsUnderwater || Character.astral || Character.InAstral || Character.IsFlying) && EquipSlots.isHeld)
            EquipSlots.isHeld = false;

        if (Time.time - lastAttack > currentAttackComboTime)
            attackCombo = 0;
        attackCombo %= 3;
        animator.SetInteger("AttackCombo", attackCombo);

        bool leftHanded;
        bool hasShield;
        int weaponType = GetWeaponType(EquipSlots.GetItem(HumanoidEquipSlots.SlotSpace.leftHand), EquipSlots.GetItem(HumanoidEquipSlots.SlotSpace.rightHand), out leftHanded, out hasShield);
                
        //Input stuff
        Vector2 input = new Vector2(Character.GetAxis("horizontal"), Character.GetAxis("vertical"));
        isStrafing = Character.GetToggle("r2Btn") && !Character.IsUnderwater && !Character.astral && !Character.InAstral && !Character.IsFlying;
        isBlocking = Character.GetToggle("l2Btn") && !Character.IsUnderwater && !Character.astral && !Character.InAstral && !Character.IsFlying;
        if ((!IsRunningCustomAnim || currentCustomAnim.canBeInterrupted) && !Character.IsUnderwater && !Character.astral && !Character.InAstral && !Character.IsFlying)
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
                if (!prevAttack && (IsRunningCustomAnim && currentCustomAnim != attackData || !IsRunningCustomAnim) && !isBlocking && weaponType > WEAPON_NONE)
                {
                    prevAttack = true;
                    if (EquipSlots.isHeld)
                    {
                        nextAnimData = attackData;

                        attackCombo++;
                        lastAttack = Time.time;
                        currentAttackComboTime = nextAnimData.executionTime + attackComboTime;
                    }
                    else
                        nextAnimData = unsheathData;
                }
            }
            else
                prevAttack = false;

            if (Character.GetToggle("r1Btn"))
            {
                if (!prevSheath && weaponType > WEAPON_NONE)
                {
                    prevSheath = true;
                    if (EquipSlots.isHeld)
                        nextAnimData = sheathData;
                    else
                        nextAnimData = unsheathData;
                }
            }
            else
                prevSheath = false;

            if (nextAnimData != null && (!IsRunningCustomAnim || (currentCustomAnim.canBeInterrupted && currentCustomAnim != nextAnimData)))
                SetCustomAnim(nextAnimData, direction);
        }

        animator.SetFloat("StrafeDir", PercentClockwise(transform.forward.xz().normalized, input.normalized));
        animator.SetInteger("WeaponType", weaponType);
        animator.SetBool("LeftHanded", leftHanded);
        animator.SetBool("HasShield", hasShield);

        //Custom anim stuff
        animator.SetLayerWeight(animator.GetLayerIndex("Upperbody"), ((IsRunningCustomAnim && currentCustomAnim.isUpperbody) || EquipSlots.isHeld) ? 1 : 0);
        animator.SetBool("BlockMain", IsRunningCustomAnim && currentCustomAnim.blockMainLayerAnim);
        animator.SetBool("BlockUpper", (IsRunningCustomAnim && currentCustomAnim.blockUpperLayerAnim) || isBlocking);
        animator.SetBool("Block", isBlocking);
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
        toggledCustomAnim = false;

        currentCustomAnim = customAnim;
        customAnimStartTime = Time.time;
        customAnimStartDir = dir;
        customAnimStartPos = transform.position.xz();
    }
    private static int GetWeaponType(ItemData leftHandItem, ItemData rightHandItem, out bool leftHanded, out bool hasShield)
    {
        WeaponData leftHandWeapon = null;
        WeaponData rightHandWeapon = null;
        if (leftHandItem is WeaponData)
            leftHandWeapon = (WeaponData)leftHandItem;
        if (rightHandItem is WeaponData)
            rightHandWeapon = (WeaponData)rightHandItem;

        leftHanded = false;
        hasShield = false;

        int weaponType = WEAPON_NONE;
        if (rightHandWeapon != null && leftHandWeapon != null)
        {
            if (rightHandWeapon.type == WeaponType.shield && leftHandWeapon.type != WeaponType.shield)
            {
                leftHanded = true;
                weaponType = (int)leftHandWeapon.type;
                hasShield = true;
            }
            else if (leftHandWeapon.type == WeaponType.shield && rightHandWeapon.type != WeaponType.shield)
            {
                weaponType = (int)rightHandWeapon.type;
                hasShield = true;
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
    public void Shoot()
    {
    }
}
