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
    public AnimationCurve speedEffect;

    [Space(10), Tooltip("The height of the ray from world 0 that will cast down and find the ground")]
    public float castingHeight = 100;

    public bool IsUnderwater { get; private set; }
    public bool IsSteppingInWater { get { return _isSteppingInWater; } }
    private bool _isSteppingInWater;
    private float percentUnderwater;
    
    public MovementState currentMovementState { get; private set; }
    private Animator animator;

    private Vector2 input;
    private bool jog;

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
        ApplyValuesToAnimator(animator, currentMovementState);

        //Check is underwater
        IsUnderwater = CheckIsUnderwater(transform, waterLevel, castingHeight, waterTip, out percentUnderwater, out _isSteppingInWater);

        float currentSpeed = GetCurrentSpeed(currentMovementState, walkSpeed, jogSpeed, swimSpeed, underwaterSpeed, trudgeEffect, percentUnderwater, IsSteppingInWater);

        //Set transform rotation
        AdjustRotation(transform, input, rotSpeed, currentSpeed, Mathf.Max(walkSpeed, jogSpeed, swimSpeed, underwaterSpeed), speedEffect, Time.deltaTime);

        //Set transform position
        AdjustPosition(transform, currentMovementState, currentSpeed, IsUnderwater, waterLevel, waterTip, castingHeight);

        //Set state
        currentMovementState = GetMovementState(input, currentMovementState, jog, IsUnderwater);
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
    private static void AdjustPosition(Transform transform, MovementState currentMovementState, float speed, bool isUnderwater, float waterLevel, float waterTip, float castingHeight)
    {
        if (!IsIdle(currentMovementState))
        {
            float positionOffset = speed * Time.deltaTime;

            transform.position += transform.forward * positionOffset;
        }

        float characterPosY;
        if (isUnderwater)
        {
            var bounds = transform.GetTotalBounds(Space.World);
            characterPosY = waterLevel - (bounds.size.y - waterTip);
        }
        else
            characterPosY = GetGroundHeightAt(transform.position.xz(), castingHeight);

        transform.position = new Vector3(transform.position.x, characterPosY, transform.position.z);
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

    private static void ApplyValuesToAnimator(Animator animator, MovementState moveState)
    {
        animator.SetInteger("State", (int)moveState);
    }
    private static MovementState GetMovementState(Vector2 currentInput, MovementState currentMovementState, bool jog, bool isUnderwater)
    {
        MovementState resultState = currentMovementState;
        if (Mathf.Abs(currentInput.x) > float.Epsilon || Mathf.Abs(currentInput.y) > float.Epsilon)
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

    private static void AdjustRotation(Transform transform, Vector2 input, float rotSpeed, float currentSpeed, float maxSpeed, AnimationCurve affect, float timestep)
    {
        if (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon)
        {
            float maxRotDiff = rotSpeed * timestep;

            float angleDiff = transform.forward.xz().GetShortestSignedAngle(input.ToCircle());
            float appliedRotDiff = angleDiff;
            if (Mathf.Abs(appliedRotDiff) > maxRotDiff)
                appliedRotDiff = maxRotDiff * Mathf.Sign(appliedRotDiff);

            appliedRotDiff = Mathf.Lerp(angleDiff, appliedRotDiff, affect.Evaluate(Mathf.Clamp01(currentSpeed / maxSpeed)));
            transform.forward = Quaternion.AngleAxis(appliedRotDiff, Vector3.up) * transform.forward;
        }
    }
}
