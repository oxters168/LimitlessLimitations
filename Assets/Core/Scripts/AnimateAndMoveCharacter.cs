using UnityEngine;
using UnityHelpers;

public class AnimateAndMoveCharacter : ValuedObject
{
    public enum MovementState { idle = 0, walk = 1, jog = 2, swimIdle = 3, swimStroke = 4, underwaterIdle = 5, underwaterStroke = 6 }

    [Tooltip("The height of the water plane in world units")]
    public float waterLevel = 7;

    [Space(10), Tooltip("The amount from the top of the character's bounds that stays above the water (in meters)")]
    public float waterTip = 0.1f;
    [Tooltip("How much being in water effects speed when walking or jogging (I don't know if trudge is actually a word, but it is now)")]
    public float trudgeEffect = 0.3f;

    [Space(10), Tooltip("In meters per second")]
    public float walkSpeed = 1.5f;
    [Tooltip("In meters per second")]
    public float jogSpeed = 4.70779f;
    [Tooltip("In meters per second")]
    public float swimSpeed = 2f;
    [Tooltip("In meters per second")]
    public float underwaterSpeed = 3f;
    [Tooltip("How fast the character turns in degrees per second")]
    public float rotSpeed = 100f;
    [Tooltip("How much the current character's speed effects their rotational speed")]
    public AnimationCurve speedEffect = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Tooltip("How long it takes to switch from and idle state to a moving state")]
    public float idleToMoveTime = 0.1f;
    [Tooltip("The angle between the previous input and the current input where the idle to move delay does not apply")]
    public float idleToMoveExempt = 45;

    [Space(10), Tooltip("The height of the ray from world 0 that will cast down and find the ground")]
    public float castingHeight = 100;

    public bool IsUnderwater { get; private set; }
    public bool IsSteppingInWater { get { return _isSteppingInWater; } }
    private bool _isSteppingInWater;
    private float percentUnderwater;
    
    public MovementState currentMovementState { get; private set; }
    private Animator animator;

    private Vector2 input;
    private Vector2 prevInput;
    private float inputStartTime;
    private bool jog;

    private CustomAnimationData currentCustomAnim;
    private float customAnimStartTime;
    public bool IsRunningCustomAnim { get { return currentCustomAnim != null && Time.time - customAnimStartTime <= currentCustomAnim.executionTime; } }
    private Vector2 customAnimStartDir;
    private Vector2 customAnimStartPos;
    private bool toggledCustomAnim;

    void Start()
    {
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        //Retrieve input
        input = new Vector2(GetAxis("dpadHor"), GetAxis("dpadVer"));
        jog = GetToggle("crossBtn");
        
        //Set animator values
        ApplyValuesToAnimator();

        //Check is underwater
        IsUnderwater = CheckIsUnderwater(transform, waterLevel, castingHeight, waterTip, out percentUnderwater, out _isSteppingInWater);

        //Set state
        currentMovementState = GetMovementState(transform, input, prevInput, idleToMoveTime, idleToMoveExempt, ref inputStartTime, currentMovementState, jog, IsUnderwater);
        
        float currentSpeed = GetCurrentSpeed(currentMovementState, walkSpeed, jogSpeed, swimSpeed, underwaterSpeed, trudgeEffect, percentUnderwater, IsSteppingInWater);

        //Set transform rotation
        AdjustRotation(currentSpeed, Mathf.Max(walkSpeed, jogSpeed, swimSpeed, underwaterSpeed));

        //Set transform position
        AdjustPosition(currentSpeed);

        prevInput = input;
    }

    public void RunCustomAnim(CustomAnimationData animData)
    {
        if (!IsRunningCustomAnim || currentCustomAnim.canBeInterrupted)
        {
            customAnimStartTime = Time.time;
            customAnimStartDir = (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) ? input.normalized : transform.forward.xz();
            customAnimStartPos = transform.position.xz();
            currentCustomAnim = animData;
        }
    }

    public static bool IsIdle(MovementState currentMovementState)
    {
        return currentMovementState == MovementState.idle || currentMovementState == MovementState.swimIdle || currentMovementState == MovementState.underwaterIdle;
    }
    private static bool CheckIsUnderwater(Transform transform, float waterLevel, float castingHeight, float waterTip, out float percentUnderwater, out bool isSteppingInWater)
    {
        float groundHeight = GetGroundHeightAt(transform.position.xz(), castingHeight);
        var characterBounds = transform.GetTotalBounds(Space.World);
        float characterNeck = groundHeight + (characterBounds.size.y - waterTip);
        percentUnderwater = Mathf.Clamp01((waterLevel - groundHeight) / (characterBounds.size.y - waterTip));
        isSteppingInWater = (groundHeight - waterLevel) < 0;
        return (characterNeck - waterLevel) <= 0;
    }
    public static float GetGroundHeightAt(Vector2 position, float castingHeight = 100)
    {
        float groundHeight = float.MinValue;
        Vector3 topPoint = new Vector3(position.x, castingHeight, position.y);
        RaycastHit hitInfo;
        if (Physics.Raycast(topPoint, Vector3.down, out hitInfo, castingHeight, LayerMask.GetMask("Ground")))
        {
            Debug.DrawRay(topPoint, Vector3.down * hitInfo.distance, Color.green);
            groundHeight = castingHeight - hitInfo.distance;
        }
        return groundHeight;
    }
    private static float GetCurrentSpeed(MovementState currentMovementState, float walkSpeed, float jogSpeed, float swimSpeed, float underwaterSpeed, float trudgeEffect, float percentUnderwater, bool isSteppingInWater)
    {
        float speed = 0;
        float trudgeMultiplier = 1 - (1 - trudgeEffect) * percentUnderwater;
        switch (currentMovementState)
        {
            case MovementState.walk:
                speed = walkSpeed * (isSteppingInWater ? trudgeMultiplier : 1);
                break;
            case MovementState.jog:
                speed = jogSpeed * (isSteppingInWater ? trudgeMultiplier : 1);
                break;
            case MovementState.swimStroke:
                speed = swimSpeed;
                break;
            case MovementState.underwaterStroke:
                speed = underwaterSpeed;
                break;
        }
        return speed;
    }

    public static float GetTransformAngle(Transform transform)
    {
        return GetAngleOf(transform.forward.xz());
    }
    public static float GetAngleOf(Vector2 dir)
    {
        float angle = Vector2.SignedAngle(dir, Vector2.up);
        if (Mathf.Abs(Mathf.Abs(angle) - 180) <= float.Epsilon)
            angle = 180;
        return angle;
    }

    private void ApplyValuesToAnimator()
    {
        animator.SetBool("CustomAnim", IsRunningCustomAnim);
        if (IsRunningCustomAnim)
        {
            if (!toggledCustomAnim)
            {
                animator.SetTrigger(currentCustomAnim.animationToggleName);
                toggledCustomAnim = true;
            }
        }
        else
            toggledCustomAnim = false;
        
        animator.SetInteger("State", (int)currentMovementState);
    }
    private static MovementState GetMovementState(Transform transform, Vector2 currentInput, Vector2 prevInput, float idleToMoveTime, float angleDelayExempt, ref float inputStartTime, MovementState currentMovementState, bool jog, bool isUnderwater)
    {
        MovementState resultState = currentMovementState;
        bool hasInput = Mathf.Abs(currentInput.x) > float.Epsilon || Mathf.Abs(currentInput.y) > float.Epsilon;
        bool prevHasInput = Mathf.Abs(prevInput.x) > float.Epsilon || Mathf.Abs(prevInput.y) > float.Epsilon;
        float angleBetween = float.MaxValue;
        if (hasInput && !prevHasInput)
        {
            inputStartTime = Time.time;
            angleBetween = Vector2.Angle(currentInput.normalized, transform.forward.xz().normalized);
        }

        if (hasInput && ((Time.time - inputStartTime) >= idleToMoveTime || angleBetween <= Mathf.Abs(angleDelayExempt)))
        {
            if (isUnderwater)
                resultState = MovementState.swimStroke;
            else if (jog)
                resultState = MovementState.jog;
            else
                resultState = MovementState.walk;
        }
        else if (isUnderwater)
            resultState = MovementState.swimIdle;
        else
            resultState = MovementState.idle;

        return resultState;
    }

    private void AdjustRotation(float currentSpeed, float maxSpeed)
    {
        bool customAnimOverride = IsRunningCustomAnim && currentCustomAnim.affectMovement;
        if (!customAnimOverride && (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon))
        {
            float maxRotDiff = rotSpeed * Time.deltaTime;

            float angleDiff = transform.forward.xz().normalized.GetShortestSignedAngle(input.normalized);
            float appliedRotDiff = angleDiff;
            if (Mathf.Abs(appliedRotDiff) > maxRotDiff)
                appliedRotDiff = maxRotDiff * Mathf.Sign(appliedRotDiff);

            appliedRotDiff = Mathf.Lerp(angleDiff, appliedRotDiff, speedEffect.Evaluate(Mathf.Clamp01(currentSpeed / maxSpeed)));
            transform.forward = Quaternion.AngleAxis(appliedRotDiff, Vector3.up) * transform.forward;
        }
        else if (customAnimOverride)
        {
            transform.forward = customAnimStartDir.ToXZVector3();
        }
    }
    private void AdjustPosition(float speed)
    {
        bool customAnimOverride = IsRunningCustomAnim && currentCustomAnim.affectMovement;

        Vector3 finalPosition = transform.position;
        if (!customAnimOverride && !IsIdle(currentMovementState))
        {
            float positionOffset = speed * Time.deltaTime;

            finalPosition += transform.forward * positionOffset;
        }
        else if (customAnimOverride)
        {
            finalPosition = customAnimStartPos.ToXZVector3() + transform.forward * currentCustomAnim.distanceTravelled * currentCustomAnim.travelMap.Evaluate((Time.time - customAnimStartTime) / currentCustomAnim.executionTime);
        }

        float characterPosY;
        if (IsUnderwater)
        {
            var bounds = transform.GetTotalBounds(Space.World);
            characterPosY = waterLevel - (bounds.size.y - waterTip);
        }
        else
            characterPosY = GetGroundHeightAt(transform.position.xz(), castingHeight);

        finalPosition = new Vector3(finalPosition.x, characterPosY, finalPosition.z);
        transform.position = finalPosition;
    }
}
