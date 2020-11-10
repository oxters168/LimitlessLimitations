using UnityEngine;
using UnityHelpers;
using Rewired;

public class PlayerMovement : MonoBehaviour
{
    public int playerId = 0;
    private Player player;
    public float turnSpeed = 0.1f;

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

        //Set state
        if (Mathf.Abs(inputDir.x) > float.Epsilon || Mathf.Abs(inputDir.y) > float.Epsilon)
        {
            if (jog)
                currentMovementState = MovementState.jog;
            else
                currentMovementState = MovementState.walk;
        }
        else
            currentMovementState = MovementState.idle;

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
        if (turn == Turn.left)
            animator.SetTrigger("TurnLeft");
        else if (turn == Turn.right)
            animator.SetTrigger("TurnRight");
        else if (turn == Turn.around)
            animator.SetTrigger("TurnAround");

        //Set transform values
        lerpPercent = Mathf.Clamp01(lerpPercent + Time.deltaTime * turnSpeed);
        transform.forward = Vector3.Lerp(fromRotDirection.ToXZVector3(), currentDirection.ToXZVector3(), lerpPercent);

        //Set prev values
        prevInputDir = inputDir;
        prevDirection = currentDirection;
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
