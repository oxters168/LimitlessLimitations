using UnityEngine;
using UnityHelpers;
using Rewired;

public class PlayerMovement : MonoBehaviour
{
    public int playerId = 0;
    private Player player;
    [Tooltip("Time in seconds to wait after turning before starting to move")]
    public float waitTimeAfterTurn = 0.2f;
    private float turnStartTime;
    [Tooltip("Revolutions per second")]
    public float turnSpeed = 0.333f;
    public float turnAroundSpeed = 0.8f;
    private float currentTurnSpeed = 0;

    public enum MovementState { idle = 0, walk = 1, jog = 2, }
    private enum Turn { none, left, right, around, }
    public MovementState currentMovementState;
    public Vector2 currentDirection = Vector2.up;
    public Vector2 prevDirection = Vector2.up;
    public Vector2 fromRotDirection = Vector2.up;
    public Vector2 inputDir;
    private Vector2 prevInputDir = Vector2.zero;
    public Animator animator;
    private float lerpPercent = 0;

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
        inputDir = new Vector2(x, y);
        bool jog = player.GetButton("Jog");
        
        //Fix input to only allow for one direction at a time
        if (Mathf.Abs(inputDir.x) > float.Epsilon && Mathf.Abs(inputDir.y) > 0)
        {
            if (Mathf.Abs(prevInputDir.x) > float.Epsilon)
                inputDir = new Vector2(x, 0);
            else
                inputDir = new Vector2(0, y);
        }

        //Get turn
        currentDirection = GetDirectionFromInput(inputDir);
        Turn turn = GetTurn(prevDirection, currentDirection);
        if (turn != Turn.none)
        {
            fromRotDirection = prevDirection;
            lerpPercent = 0;
        }

        //Set animator values
        animator.SetInteger("State", (int)currentMovementState);
        animator.ResetTrigger("TurnLeft");
        animator.ResetTrigger("TurnRight");
        animator.ResetTrigger("TurnAround");
        if (turn == Turn.left)
            animator.SetTrigger("TurnLeft");
        else if (turn == Turn.right)
            animator.SetTrigger("TurnRight");
        else if (turn == Turn.around)
            animator.SetTrigger("TurnAround");

        //Set transform values
        if (turn != Turn.none)
        {
            if (turn == Turn.around)
                currentTurnSpeed = turnAroundSpeed;
            else
                currentTurnSpeed = turnSpeed;

            if (currentMovementState == MovementState.idle)
                turnStartTime = Time.time;
        }
        float currentAngle = GetCurrentTransformAngle();
        float targetAngle = GetAngleOf(currentDirection);
        float angleDiff = targetAngle - currentAngle;
        if (Mathf.Abs(angleDiff) > 180)
            angleDiff = -angleDiff % 180; //Flip it around to go the shorter way (abs(value) can never be more than 180)
        float step = Time.deltaTime * (currentTurnSpeed * 360);
        if (Mathf.Abs(angleDiff) < step)
            currentAngle = targetAngle;
        else
            currentAngle += step * Mathf.Sign(angleDiff);

        transform.forward = Vector2.up.Rotate(currentAngle).ToXZVector3();

        //Set state
        if (Mathf.Abs(inputDir.x) > float.Epsilon || Mathf.Abs(inputDir.y) > float.Epsilon)
        {
            if (Time.time - turnStartTime > waitTimeAfterTurn)
            {
                if (jog)
                    currentMovementState = MovementState.jog;
                else
                    currentMovementState = MovementState.walk;
            }
        }
        else
            currentMovementState = MovementState.idle;

        //Set prev values
        prevInputDir = inputDir;
        prevDirection = currentDirection;
    }

    public float GetCurrentTransformAngle()
    {
        return GetAngleOf(transform.forward.xz());
        //return transform.eulerAngles.y;
    }
    public static float GetAngleOf(Vector2 dir)
    {
        float angle = Vector2.SignedAngle(dir, Vector2.up);
        if (Mathf.Abs(Mathf.Abs(angle) - 180) <= float.Epsilon)
            angle = 180;
        //if (angle < 0)
        //    angle += 360;
        return angle;
    }
    public Vector2 GetDirectionFromInput(Vector2 input)
    {
        if (Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon)
            return input;
        else
            return currentDirection;
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
}
