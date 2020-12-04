using UnityEngine;
using UnityHelpers;

public class AnimateAndMoveCharacter : ValuedObject
{
    public enum MovementState { idle = 0, walk = 1, jog = 2, swimIdle = 3, swimStroke = 4, underwaterIdle = 5, underwaterStroke = 6, flyIdle = 7, flyFwd = 8, lieDown = 9, standUp = 10, sleep = 11 }

    [Tooltip("The main shown renderer of the character")]
    public Skinwalker mainShells;
    [Tooltip("The dummy renderer of the character (used for astral projection)")]
    public Skinwalker dummyShells;

    [Space(10), Tooltip("The height of the water plane in world units")]
    public float waterLevel = 7;

    [Space(10), Tooltip("The amount from the top of the character's bounds that stays above the water (in meters)")]
    public float waterTip = 1;
    [Tooltip("How much being in water effects speed when walking or jogging (I don't know if trudge is actually a word, but it is now)")]
    public float trudgeEffect = 0.3f;

    private float currentSpeed;
    [Space(10), Tooltip("In meters per second")]
    public float walkSpeed = 1.5f;
    [Tooltip("In meters per second")]
    public float jogSpeed = 4.70779f;
    [Tooltip("In meters per second")]
    public float swimSpeed = 2f;
    [Tooltip("In meters per second")]
    public float underwaterSpeed = 3f;
    [Tooltip("In meters per second")]
    public float flySpeed = 20;
    [Tooltip("How fast the character can go up/down when flying (in meters per second)")]
    public float ascendDescendSpeed = 20;
    [Tooltip("How fast the character turns in degrees per second")]
    public float rotSpeed = 100f;
    [Tooltip("How much the current character's speed effects their rotational speed")]
    public AnimationCurve speedEffect = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Tooltip("How long it takes to switch from and idle state to a moving state")]
    public float idleToMoveTime = 0.1f;
    [Tooltip("The angle between the previous input and the current input where the idle to move delay does not apply")]
    public float idleToMoveExempt = 45;

    // [Space(10), Tooltip("The material to be used on the character shell when astral set to true")]
    // public Material astralMaterial;
    [Tooltip("Sets the character into the astral form")]
    public bool astral;
    [Tooltip("The time it takes to enter/exit the astral body (in seconds)")]
    public float astralShiftDuration = 2;
    private float astralShiftStart = -1;
    public bool InAstral { get; private set; }
    // private HumanoidShell currentShell = null;

    // [Space(10), Tooltip("Character will lie down and be unable to move if set to true")]
    // public bool asleep;

    [Space(10), Tooltip("If true the character will be able to fly during astral projection")]
    public bool canFly;
    public bool IsFlying { get; private set; }
    [Space(10), Tooltip("The minimum height the character flies from the ground")]
    public float flyMinHeight = 1.5f;
    [Tooltip("The maximum height the character can fly up to")]
    public float flyMaxHeight = 100f;
    private bool groundDown;

    [Space(10), Tooltip("The height of the ray from world 0 that will cast down and find the ground")]
    public float castingHeight = 100;
    // [Tooltip("The radius of the sphere that will cushion the character from other objects")]
    // public float collisionRadius = 3;

    [Space(10), Tooltip("Blocks the input from moving the character")]
    public bool blockMovement;
    [Tooltip("Blocks input from rotating the character")]
    public bool blockRotation;

    public bool IsUnderwater { get; private set; }
    public bool IsSteppingInWater { get { return _isSteppingInWater; } }
    private bool _isSteppingInWater;
    private float percentUnderwater;
    
    public MovementState currentMovementState { get; private set; }

    private Vector2 input;
    private Vector2 prevInput;
    private float inputStartTime;
    private bool jog;
    public bool up, down;

    private Vector2 prevPosition;
    private bool isColliding;

    private HumanoidEquipSlots equipSlots;

    void Start()
    {
        equipSlots = GetComponentInChildren<HumanoidEquipSlots>();
    }
    void Update()
    {
        //Retrieve input
        input = new Vector2(GetAxis("horizontal"), GetAxis("vertical"));
        jog = GetToggle("crossBtn");
        up = GetToggle("r2Btn");
        down = GetToggle("l2Btn");
        
        RefreshAstralStatus();
        RefreshFlightStatus();
        RefreshAstralAppearance();
        // RefreshShellMaterial();
        if (equipSlots != null)
            equipSlots.SetVisible(!(astral && InAstral));
        
        //Set animator values
        ApplyValuesToAnimator();

        //Check is underwater
        IsUnderwater = !IsFlying && CheckIsUnderwater(mainShells.transform, waterLevel, castingHeight, waterTip, out percentUnderwater, out _isSteppingInWater);

        //Set state
        currentMovementState = GetMovementState(mainShells.transform, input, prevInput, idleToMoveTime, idleToMoveExempt, ref inputStartTime, currentMovementState, IsFlying, jog, IsUnderwater, astral, InAstral);
        
        float targetSpeed = GetCurrentSpeed(currentMovementState, walkSpeed, jogSpeed, swimSpeed, underwaterSpeed, flySpeed, trudgeEffect, percentUnderwater, IsSteppingInWater);

        //Affect speed based on joystick if flying
        if (IsFlying)
        {
            float inputAffect = input.ToCircle().magnitude;
            targetSpeed *= inputAffect;
        }
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5);

        //Set transform rotation
        AdjustRotation(currentSpeed, Mathf.Max(walkSpeed, jogSpeed, swimSpeed, underwaterSpeed));

        //Set transform position
        AdjustPosition(currentSpeed);

        prevInput = input;
        isColliding = false;
    }
    public void OnTriggerStay(Collider other)
    {
        isColliding = true;

        var characterBounds = mainShells.transform.GetTotalBounds(Space.World);
        var otherBounds = other.transform.GetTotalBounds(Space.World);
        Vector2 offset = characterBounds.center.xz() - otherBounds.center.xz();
        Vector2 dir = (offset).normalized;
        
        float distance = (mainShells.transform.position.xz() - prevPosition).magnitude;
        if (distance < float.Epsilon)
            distance = 0.1f;
        Vector2 borderPosition = mainShells.transform.position.xz() + dir * distance;

        // Vector2 breach = characterBounds.center.xz() - otherBounds.center.xz();

        // Vector2 borderPosition = transform.position.xz();
        // float xOffset = Mathf.Abs(breach.x) - otherBounds.extents.x;
        // float yOffset = Mathf.Abs(breach.y) - otherBounds.extents.z;
        // bool xAxis = false;
        // if (xOffset < -float.Epsilon && yOffset < -float.Epsilon)
        //     xAxis = true;
        // else if (yOffset < -float.Epsilon)
        //     xAxis = true;
        
        // if (xAxis)
        //     borderPosition.x = otherBounds.center.x + (otherBounds.extents.x + collisionRadius) * Mathf.Sign(breach.x) * 1.001f;
        // else
        //     borderPosition.y = otherBounds.center.z + (otherBounds.extents.z + collisionRadius) * Mathf.Sign(breach.y) * 1.001f;

        // Vector3 axis0 = worldAxes[0];
        // Vector3 axis1 = worldAxes[1];
        // float axis0Mag = otherBounds.extents.Multiply(axis0).magnitude;
        // float axis1Mag = otherBounds.extents.Multiply(axis1).magnitude;
        // Debug.DrawRay(otherBounds.center, axis0 * axis0Mag, Color.blue, 1);
        // Debug.DrawRay(otherBounds.center, axis1 * axis1Mag, Color.green, 1);
        // Debug.DrawRay(otherBounds.center, dir, Color.white, 1);


        // float theta = Vector2.up.GetClockwiseAngle(dir.xz().normalized);
        
        // Vector3 axis0 = worldAxes[0];
        // Vector3 axis1 = worldAxes[1];
        // if ((theta <= 135 && theta >= 45) || (theta >= 225 && theta <= 315))
        // {
        //     axis0 = worldAxes[1];
        //     axis1 = worldAxes[0];
        // }
        // else
        // {
        //     axis0 = worldAxes[0];
        //     axis1 = worldAxes[1];
        // }
        // float a = otherBounds.extents.Multiply(axis0).magnitude;
        // float b = otherBounds.extents.Multiply(axis1).magnitude;
        // // float x = a * Mathf.Cos(theta * Mathf.Deg2Rad);
        // // float y = b * Mathf.Sin(theta * Mathf.Deg2Rad);
        // float tanTheta = Mathf.Tan(theta * Mathf.Deg2Rad);
        // float x = (a * b) / Mathf.Sqrt(b * b + a * a * tanTheta * tanTheta);
        // float y = (a * b) / Mathf.Sqrt(a * a + (b * b) / (tanTheta * tanTheta));
        // // float x = a * dir.Multiply(axis0).magnitude;
        // // float y = b * dir.Multiply(axis1).magnitude;
        // // float radius = (x * x) / (a * a) + (y * y) / (b * b) - 1;
        // float radius = Mathf.Sqrt(x * x + y * y);
        // Vector3 borderPosition = otherBounds.center + dir * radius;
        // // Vector3 borderPosition = otherBounds.center + axis0 * x * axis0Mag + axis1 * y * axis1Mag;
        // // Vector3 borderPosition = otherBounds.center + dir.Multiply(axis0) * axis0Mag + dir.Multiply(axis1) * axis1Mag;

        // Debug.DrawLine(otherBounds.center, borderPosition, Color.black, 5);
        SetPosition(borderPosition);
    }

    private void RefreshAstralAppearance()
    {
        bool dummyShowing = astral && InAstral;
        if (!dummyShowing)
        {
            dummyShells.transform.position = mainShells.transform.position;
            dummyShells.transform.rotation = mainShells.transform.rotation;
        }
        dummyShells.gameObject.SetActive(dummyShowing);
        mainShells.materialIndex = astral && InAstral ? 0 : -1;
    }
    private void RefreshAstralStatus()
    {
        if (astral)
        {
            if (!InAstral)
            {
                blockMovement = true;
                blockRotation = true;

                if (astralShiftStart < 0)
                    astralShiftStart = Time.time;
                if (Time.time - astralShiftStart >= astralShiftDuration)
                {
                    InAstral = true;

                    astralShiftStart = -1;
                    blockMovement = false;
                    blockRotation = false;
                }
            }
        }
        else
        {
            if (InAstral)
            {
                blockMovement = true;
                blockRotation = true;

                if (astralShiftStart < 0)
                    astralShiftStart = Time.time;
                if (Time.time - astralShiftStart >= astralShiftDuration)
                {
                    InAstral = false;

                    astralShiftStart = -1;
                    blockMovement = false;
                    blockRotation = false;
                }
            }
        }
    }
    private void RefreshFlightStatus()
    {
        if (InAstral)
        {
            if (canFly && !IsFlying && up)
            {
                IsFlying = true;
            }

            if (down)
            {
                if (!groundDown)
                {
                    groundDown = true;
                    if (IsFlying && (mainShells.transform.position.y - (GetGroundHeightAt(mainShells.transform.position.xz(), castingHeight) + flyMinHeight)) <= float.Epsilon)
                    {
                        IsFlying = false;
                    }
                }
            }
            else
                groundDown = false;
        }
        else
            IsFlying = false;
    }
    public static bool IsIdle(MovementState currentMovementState)
    {
        return currentMovementState == MovementState.idle || currentMovementState == MovementState.swimIdle || currentMovementState == MovementState.underwaterIdle;
    }
    private static bool CheckIsUnderwater(Transform transform, float waterLevel, float castingHeight, float waterTip, out float percentUnderwater, out bool isSteppingInWater)
    {
        float groundHeight = GetGroundHeightAt(transform.position.xz(), castingHeight);
        var characterBounds = transform.GetTotalBounds(Space.Self);
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
    private static float GetCurrentSpeed(MovementState currentMovementState, float walkSpeed, float jogSpeed, float swimSpeed, float underwaterSpeed, float flySpeed, float trudgeEffect, float percentUnderwater, bool isSteppingInWater)
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
            case MovementState.flyFwd:
                speed = flySpeed;
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
        mainShells.CurrentAnimator.SetInteger("State", (int)currentMovementState);
        if (dummyShells.gameObject.activeInHierarchy)
            dummyShells.CurrentAnimator.SetInteger("State", (int)MovementState.sleep);
    }
    private static MovementState GetMovementState(Transform transform, Vector2 currentInput, Vector2 prevInput, float idleToMoveTime, float angleDelayExempt, ref float inputStartTime, MovementState currentMovementState, bool fly, bool jog, bool isUnderwater, bool astral, bool inAstral)
    {
        MovementState resultState = currentMovementState;
        bool hasInput = !currentInput.IsZero();
        bool prevHasInput = !prevInput.IsZero();
        float angleBetween = float.MaxValue;
        if (hasInput && !prevHasInput)
        {
            inputStartTime = Time.time;
            angleBetween = Vector2.Angle(currentInput.normalized, transform.forward.xz().normalized);
        }

        if (hasInput && ((Time.time - inputStartTime) >= idleToMoveTime || angleBetween <= Mathf.Abs(angleDelayExempt)))
        {
            if (fly)
                resultState = MovementState.flyFwd;
            else if (isUnderwater)
                resultState = MovementState.swimStroke;
            else if (jog)
                resultState = MovementState.jog;
            else
                resultState = MovementState.walk;
        }
        else if (fly)
            resultState = MovementState.flyIdle;
        else if (isUnderwater)
            resultState = MovementState.swimIdle;
        else
            resultState = MovementState.idle;

        if (astral != inAstral)
        {
            if (astral && !inAstral)
                resultState = MovementState.lieDown;
            else
                resultState = MovementState.standUp;
        }

        return resultState;
    }

    private void AdjustRotation(float currentSpeed, float maxSpeed)
    {
        if (!blockRotation && (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon))
        {
            float maxRotDiff = rotSpeed * Time.deltaTime;

            float angleDiff = mainShells.transform.forward.xz().normalized.GetShortestSignedAngle(input.normalized);
            float appliedRotDiff = angleDiff;
            if (Mathf.Abs(appliedRotDiff) > maxRotDiff)
                appliedRotDiff = maxRotDiff * Mathf.Sign(appliedRotDiff);

            appliedRotDiff = Mathf.Lerp(angleDiff, appliedRotDiff, speedEffect.Evaluate(Mathf.Clamp01(currentSpeed / maxSpeed)));
            mainShells.transform.forward = Quaternion.AngleAxis(appliedRotDiff, Vector3.up) * mainShells.transform.forward;
        }
    }
    private void AdjustPosition(float speed)
    {
        Vector3 finalPosition = mainShells.transform.position;
        if (!blockMovement && !isColliding && !IsIdle(currentMovementState))
        {
            float positionOffset = speed * Time.deltaTime;

            finalPosition += mainShells.transform.forward * positionOffset;
        }

        SetPosition(finalPosition.xz());
    }
    public void SetPosition(Vector2 position)
    {
        float characterPosY = GetY(position);

        Vector3 nextPosition = new Vector3(position.x, characterPosY, position.y);

        prevPosition = mainShells.transform.position.xz();
        mainShells.transform.position = nextPosition;
    }
    private float GetY(Vector2 position)
    {
        float characterPosY;
        if (IsFlying)
        {
            float nextY = mainShells.transform.position.y;
            if (up)
                nextY += ascendDescendSpeed * Time.deltaTime;
            else if (down)
                nextY -= ascendDescendSpeed * Time.deltaTime;

            float groundHeight = GetGroundHeightAt(position, castingHeight);
            nextY = Mathf.Max(groundHeight + flyMinHeight, waterLevel + flyMinHeight, nextY);
            nextY = Mathf.Min(nextY, flyMaxHeight);
            characterPosY = nextY;
        }
        else if (IsUnderwater)
        {
            var bounds = mainShells.transform.GetTotalBounds(Space.Self);
            characterPosY = waterLevel - (bounds.size.y - waterTip);
        }
        else
            characterPosY = GetGroundHeightAt(position, castingHeight);
            
        return characterPosY;
    }
}
