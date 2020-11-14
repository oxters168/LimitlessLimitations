using UnityEngine;
using UnityHelpers;
using Rewired;

public class AnimateAndMoveCharacter : MonoBehaviour, IValueManager
{
    public enum MovementState { idle = 0, walk = 1, jog = 2, swimIdle = 3, swimStroke = 4, underwaterIdle = 5, underwaterStroke = 6 }
    private enum Turn { none, left, right, around, }

    [Tooltip("The height of the water plane in world units")]
    public float waterLevel = 7;
    [Tooltip("The grid cell size in meters")]
    public float gridCellSize = 1;
    public Vector2Int gridIndex = Vector2Int.zero;
    private Vector2 characterMoveInput;
    private Vector2 prevInputDir = Vector2.zero;

    [Space(10), Tooltip("The amount from the top of the character's bounds that stays above the water (in meters)")]
    public float waterTip = 0.1f;
    [Tooltip("How much being in water effects speed when walking or jogging (I don't know if trudge is actually a word, but it is now)")]
    public float trudgeEffect = 0.3f;
    private float percentUnderwater;

    [Space(10), Tooltip("In meters per second")]
    public float walkSpeed = 1.5f;
    [Tooltip("In meters per second")]
    public float jogSpeed = 4.70779f;
    [Tooltip("In meters per second")]
    public float swimSpeed = 2f;
    [Tooltip("In meters per second")]
    public float underwaterSpeed = 3f;

    [Space(10), Tooltip("The time it takes to turn 90 degrees measured in revolutions per second")]
    public float turnSpeed = 0.8f;
    [Tooltip("The time it takes to turn 180 degrees measured in revolutions per second")]
    public float turnAroundSpeed = 1f;
    private float currentTurnSpeed = 0;
    private float expectedTurnTime = 0;
    private float turnStartTime = float.MinValue;

    [Space(10), Tooltip("The height of the ray from world 0 that will cast down and find the ground")]
    public float castingHeight = 100;

    public bool isUnderwater { get; private set; }
    public bool isSteppingInWater { get; private set; }
    public MovementState currentMovementState { get; private set; }
    private Vector2 _direction = Vector2.up;
    public Vector2 Direction { get { return _direction; } private set { _direction = value; } }
    private Vector2 prevDirection = Vector2.up;
    private Animator animator;

    private Vector2 input;
    private bool jog;
    [Space(10)]
    public ValuesVault values;

    void Start()
    {
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        //Retrieve input
        input = new Vector2(GetAxis("dpadHor"), GetAxis("dpadVer"));
        jog = GetToggle("crossBtn");
        
        //Fix input to only allow for one direction at a time
        input = FixToOneAxis(input, prevInputDir);
        characterMoveInput = OverrideForGrid(input, gridIndex, transform.position.xz(), Direction, gridCellSize);
        if (!IsIdle() && input.x == characterMoveInput.x && input.y == characterMoveInput.y)
            gridIndex = GetNextGridIndex();

        //Get turn
        Direction = GetDirectionFromInput(characterMoveInput);
        Turn turnState = GetTurn(prevDirection, Direction);

        //Set animator values
        ApplyValuesToAnimator(animator, currentMovementState, turnState);

        //Check is underwater
        CheckIsUnderwater();

        //Set transform rotation
        AdjustRotation(turnState);

        //Set transform position
        AdjustPosition();

        //Set state
        currentMovementState = GetMovementState(characterMoveInput);

        //Set prev values
        prevInputDir = characterMoveInput;
        prevDirection = Direction;
    }
    void OnDrawGizmos()
    {
        var characterBounds = transform.GetTotalBounds(Space.World);
        Gizmos.DrawWireCube(characterBounds.center, characterBounds.size);
    }

    public bool IsIdle()
    {
        return currentMovementState == MovementState.idle || currentMovementState == MovementState.swimIdle || currentMovementState == MovementState.underwaterIdle;
    }
    private void CheckIsUnderwater()
    {
        float groundHeight = GetGroundHeightAt(transform.position.xz(), castingHeight);
        var characterBounds = transform.GetTotalBounds(Space.World);
        float characterNeck = groundHeight + (characterBounds.size.y - waterTip);
        percentUnderwater = Mathf.Clamp01((waterLevel - groundHeight) / (characterBounds.size.y - waterTip));
        isSteppingInWater = (groundHeight - waterLevel) < 0;
        isUnderwater = (characterNeck - waterLevel) <= 0;
    }
    public static Vector2Int CalculateGridIndex(Vector2 position, Vector2 direction, float gridCellSize)
    {
        float x = position.x / gridCellSize;
        float y = position.y / gridCellSize;
        int xInt;
        int yInt;
        if ((x < 0 || x > 0) && direction.x < 0)
            xInt = Mathf.CeilToInt(x);
        else
            xInt = Mathf.FloorToInt(x);
        if ((y < 0 || y > 0) && direction.y < 0)
            yInt = Mathf.CeilToInt(y);
        else
            yInt = Mathf.FloorToInt(y);
        return new Vector2Int(xInt, yInt);
    }
    public Vector2Int GetNextGridIndex()
    {
        var nextGridIndex = CalculateGridIndex(transform.position.xz(), Direction, gridCellSize);
        bool xIsPressed = Mathf.Abs(input.x) > float.Epsilon;
        bool yIsPressed = Mathf.Abs(input.y) > float.Epsilon;
        if (xIsPressed)
            nextGridIndex = new Vector2Int(nextGridIndex.x + (int)Mathf.Sign(input.x), nextGridIndex.y);
        if (yIsPressed)
            nextGridIndex = new Vector2Int(nextGridIndex.x, nextGridIndex.y + (int)Mathf.Sign(input.y));
            
        if (!xIsPressed && !yIsPressed)
            nextGridIndex = gridIndex;

        return nextGridIndex;
    }
    public static Vector2 GetIndexPosition(Vector2Int index, float gridCellSize)
    {
        return new Vector2(index.x * gridCellSize, index.y * gridCellSize);
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
    private void AdjustPosition()
    {
        Vector2 expectedPosition = GetIndexPosition(gridIndex, gridCellSize);
        if (!IsIdle())
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
            
            float positionOffset = speed * Time.deltaTime;

            Vector2 outOfWhack = expectedPosition - transform.position.xz();
            if (Mathf.Abs(Direction.x) > float.Epsilon && positionOffset > Mathf.Abs(outOfWhack.x))
                positionOffset = Mathf.Abs(outOfWhack.x);
            else if (Mathf.Abs(Direction.y) > float.Epsilon && positionOffset > Mathf.Abs(outOfWhack.y))
                positionOffset = Mathf.Abs(outOfWhack.y);

            transform.position += Direction.ToXZVector3() * positionOffset;
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

    public float GetCurrentTransformAngle()
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
    public Vector2 GetDirectionFromInput(Vector2 input)
    {
        if (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon)
            return input;
        else
            return Direction;
    }
    private static Turn GetTurn(Vector2 from, Vector2 to)
    {
        Turn tt;
        float xShift = to.x - from.x;
        float yShift = to.y - from.y;
        float angle = Vector2.SignedAngle(to, from);
        if (Mathf.Abs(xShift) > 1.5f || Mathf.Abs(yShift) > 1.5f)
            tt = Turn.around;
        else if (angle < -float.Epsilon)
            tt = Turn.left;
        else if (angle > float.Epsilon)
            tt = Turn.right;
        else
            tt = Turn.none;
        
        return tt;
    }

    private static Vector2 OverrideForGrid(Vector2 currentInput, Vector2Int expectedIndex, Vector2 position, Vector2 direction, float gridCellSize)
    {
        var currentIndex = CalculateGridIndex(position, direction, gridCellSize);
        // Vector2 expectedPosition = new Vector2(expectedIndex.x * gridCellSize, expectedIndex.y * gridCellSize);

        // Vector2 outOfWhack = expectedPosition - position;
        if (expectedIndex.x != currentIndex.x && Mathf.Abs(currentInput.x) <= float.Epsilon)
            currentInput = new Vector2(Mathf.Sign(expectedIndex.x - currentIndex.x), 0);
        else if (expectedIndex.y != currentIndex.y && Mathf.Abs(currentInput.y) <= float.Epsilon)
            currentInput = new Vector2(0, Mathf.Sign(expectedIndex.y - currentIndex.y));
        
        return currentInput;
    }
    private static Vector2 FixToOneAxis(Vector2 currentInput, Vector2 prevInput)
    {
        if (Mathf.Abs(currentInput.x) > float.Epsilon && Mathf.Abs(currentInput.y) > 0)
        {
            if (Mathf.Abs(prevInput.x) > float.Epsilon)
                currentInput = new Vector2(currentInput.x, 0);
            else
                currentInput = new Vector2(0, currentInput.y);
        }
        return currentInput;
    }
    private static void ApplyValuesToAnimator(Animator animator, MovementState moveState, Turn turnState)
    {
        animator.SetInteger("State", (int)moveState);
        animator.ResetTrigger("TurnLeft");
        animator.ResetTrigger("TurnRight");
        animator.ResetTrigger("TurnAround");
        if (turnState == Turn.left)
            animator.SetTrigger("TurnLeft");
        else if (turnState == Turn.right)
            animator.SetTrigger("TurnRight");
        else if (turnState == Turn.around)
            animator.SetTrigger("TurnAround");
    }
    private MovementState GetMovementState(Vector2 currentInput)
    {
        MovementState resultState = currentMovementState;
        if (Mathf.Abs(currentInput.x) > float.Epsilon || Mathf.Abs(currentInput.y) > float.Epsilon)
        {
            if (Time.time - turnStartTime > expectedTurnTime)
            {
                if (isUnderwater)
                    resultState = MovementState.swimStroke;
                else if (jog)
                    resultState = MovementState.jog;
                else
                    resultState = MovementState.walk;
            }
        }
        else if (isUnderwater)
            resultState = MovementState.swimIdle;
        else
            resultState = MovementState.idle;

        return resultState;
    }

    private void AdjustRotation(Turn turn)
    {
        if (turn != Turn.none)
        {
            if (turn == Turn.around)
            {
                currentTurnSpeed = turnAroundSpeed;
                expectedTurnTime = 0.5f / currentTurnSpeed;
            }
            else
            {
                currentTurnSpeed = turnSpeed;
                expectedTurnTime = 0.25f / currentTurnSpeed;
            }

            if (IsIdle())
                turnStartTime = Time.time;
        }
        float currentAngle = GetCurrentTransformAngle();
        float targetAngle = GetAngleOf(Direction);
        float angleDiff = targetAngle - currentAngle;
        if (Mathf.Abs(angleDiff) > 180)
            angleDiff = -angleDiff % 180; //Flip it around to go the shorter way (abs(value) can never be more than 180)
        float step = Time.deltaTime * (currentTurnSpeed * 360);
        if (Mathf.Abs(angleDiff) < step)
            currentAngle = targetAngle;
        else
            currentAngle += step * Mathf.Sign(angleDiff);

        transform.forward = Vector2.up.Rotate(currentAngle).ToXZVector3();
    }

    public void SetAxis(string name, float value)
    {
        values.GetValue(name).SetAxis(value);
    }
    public float GetAxis(string name)
    {
        return values.GetValue(name).GetAxis();
    }
    public void SetToggle(string name, bool value)
    {
        values.GetValue(name).SetToggle(value);
    }
    public bool GetToggle(string name)
    {
        return values.GetValue(name).GetToggle();
    }
    public void SetDirection(string name, Vector3 value)
    {
        values.GetValue(name).SetDirection(value);
    }
    public Vector3 GetDirection(string name)
    {
        return values.GetValue(name).GetDirection();
    }
    public void SetPoint(string name, Vector3 value)
    {
        values.GetValue(name).SetPoint(value);
    }
    public Vector3 GetPoint(string name)
    {
        return values.GetValue(name).GetPoint();
    }
    public void SetOrientation(string name, Quaternion value)
    {
        values.GetValue(name).SetOrientation(value);
    }
    public Quaternion GetOrientation(string name)
    {
        return values.GetValue(name).GetOrientation();
    }
}
