using UnityEngine;
using UnityHelpers;

public class HumanoidCustomMoves : MonoBehaviour
{
    private AnimateAndMoveCharacter _character;
    private AnimateAndMoveCharacter Character { get { if (_character == null) _character = GetComponent<AnimateAndMoveCharacter>(); return _character; } }

    private Animator _animator;
    private Animator animator { get { if (_animator == null) _animator = GetComponent<Animator>(); return _animator; } }

    [Tooltip("How fast the character moves when strafing (in meters per second)")]
    public float strafeSpeed = 3.2f;
    private bool isStrafing;

    public CustomAnimationData rollData;
    public CustomAnimationData attackData;
    private bool prevRoll;
    private bool prevAttack;

    private CustomAnimationData currentCustomAnim;
    private float customAnimStartTime;
    public bool IsRunningCustomAnim { get { return currentCustomAnim != null && CustomAnimPercentPlayed <= 1; } }
    public float CustomAnimPercentPlayed { get { return currentCustomAnim != null ? (Time.time - customAnimStartTime) / currentCustomAnim.executionTime : float.MinValue; } }
    private Vector2 customAnimStartDir;
    private Vector2 customAnimStartPos;
    private bool toggledCustomAnim;

    void Update()
    {
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
                    {
                        direction = input.normalized;
                        // SetCustomAnim(rollData, input.normalized);
                    }
                    // else
                    //     SetCustomAnim(rollData);
                }
            }
            else
                prevRoll = false;

            if (Character.GetToggle("squareBtn"))
            {
                if (!prevAttack)
                {
                    prevAttack = true;
                    // SetCustomAnim(attackData);
                    nextAnimData = attackData;
                }
            }
            else
                prevAttack = false;

            // if (nextAnimData != null && (!IsRunningCustomAnim || currentCustomAnim != nextAnimData))
            if (nextAnimData != null && (!IsRunningCustomAnim || (currentCustomAnim.canBeInterrupted && currentCustomAnim != nextAnimData)))
                SetCustomAnim(nextAnimData, direction);
        }

        animator.SetFloat("StrafeDir", PercentClockwise(transform.forward.xz().normalized, input.normalized));

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
        float angleOffset = first.GetShortestSignedAngle(second);
        if (angleOffset < 0)
            angleOffset += 360;
        return angleOffset / 360;
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
}
