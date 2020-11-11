using UnityEngine;
using UnityHelpers;
using Rewired;

public class AnimateAndMoveCharacter : MonoBehaviour
{
    public enum MovementState { idle = 0, walk = 1, jog = 2, }
    private enum Turn { none, left, right, around, }

    // public Gaia.TerrainLoaderManager tlm;
    // public TerrainData td;

    public int playerId = 0;
    private Player player;
    public Vector2 input;
    private Vector2 prevInputDir = Vector2.zero;
    public bool jog;

    [Space(10), Tooltip("In meters per second")]
    public float walkSpeed = 1.5f;
    [Tooltip("In meters per second")]
    public float jogSpeed = 4.70779f;

    [Space(10), Tooltip("The time it takes to turn 90 degrees measured in revolutions per second")]
    public float turnSpeed = 0.8f;
    [Tooltip("The time it takes to turn 180 degrees measured in revolutions per second")]
    public float turnAroundSpeed = 1f;
    private float currentTurnSpeed = 0;
    private float expectedTurnTime = 0;
    private float turnStartTime = float.MinValue;

    [Space(10), Tooltip("The height from world 0 to cast the ray down and find the ground")]
    public float castingHeight = 100;


    public MovementState currentMovementState { get; private set; }
    private Vector2 _direction = Vector2.up;
    public Vector2 Direction { get { return _direction; } private set { _direction = value; } }
    private Vector2 prevDirection = Vector2.up;
    private Animator animator;

    void Start()
    {
        player = ReInput.players.GetPlayer(playerId);
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        //Retrieve input
        float x = 0;
        x += player.GetButton("Horizontal") ? 1 : 0;
        x -= player.GetNegativeButton("Horizontal") ? 1 : 0;
        float y = 0;
        y += player.GetButton("Vertical") ? 1 : 0;
        y -= player.GetNegativeButton("Vertical") ? 1 : 0;
        input = new Vector2(x, y);
        jog = player.GetButton("Jog");
        
        //Fix input to only allow for one direction at a time
        input = FixToOneAxis(input, prevInputDir);

        //Get turn
        Direction = GetDirectionFromInput(input);
        Turn turnState = GetTurn(prevDirection, Direction);

        //Set animator values
        ApplyValuesToAnimator(animator, currentMovementState, turnState);

        //Set transform rotation
        AdjustRotation(turnState);

        //Set transform position
        AdjustPosition();

        //Set state
        currentMovementState = GetMovementState(input);

        //Set prev values
        prevInputDir = input;
        prevDirection = Direction;
    }

    private void AdjustPosition()
    {
        // float height = td.GetHeight(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        // float height = td.GetInterpolatedHeight(transform.position.x / td.bounds.size.x, transform.position.z / td.bounds.size.z);
        // float height = tlm.WorldMapTerrain.SampleHeight(transform.position);
        // transform.position.Set(transform.position.x, height, transform.position.y);
        // Bounds characterBounds = transform.GetTotalBounds(Space.World);
        // Vector3 bottomPoint = characterBounds.center + Vector3.down * characterBounds.extents.y;
        Vector3 topPoint = new Vector3(transform.position.x, castingHeight, transform.position.z);
        RaycastHit hitInfo;
        if (Physics.Raycast(topPoint, Vector3.down, out hitInfo))
        {
            // Debug.Log(hitInfo.distance);
            // Gaia.TerrainLoaderManager tlm;
            // tlm.WorldMapTerrain.SampleHeight(Vector3.zero);
            Debug.DrawRay(topPoint, Vector3.down * hitInfo.distance, Color.green);
            transform.position = new Vector3(transform.position.x, castingHeight - hitInfo.distance, transform.position.z);
        }

        if (currentMovementState != MovementState.idle)
        {
            float speed;
            if (currentMovementState == MovementState.walk)
                speed = walkSpeed;
            else
                speed = jogSpeed;
            
            transform.position += Direction.ToXZVector3() * speed * Time.deltaTime;
        }
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
                if (jog)
                    resultState = MovementState.jog;
                else
                    resultState = MovementState.walk;
            }
        }
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

            if (currentMovementState == MovementState.idle)
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
}
