using UnityEngine;
using UnityHelpers;

public class HumanoidAI : MonoBehaviour
{
    public Transform target;

    [Space(10), Tooltip("The distance from the target where the AI starts to walk")]
    public float walkDistance = 3;
    [Tooltip("The distance from the target where the AI stops")]
    public float stopDistance = 0.01f;

    private AnimateAndMoveCharacter _inputAI;
    private AnimateAndMoveCharacter InputAI { get { if (_inputAI == null) _inputAI = GetComponent<AnimateAndMoveCharacter>(); return _inputAI; } }

    void Update()
    {
        Vector2 input = Vector2.zero;
        bool run = false;
        if (target != null)
        {
            Vector2 diff = target.position.xz() - InputAI.mainShells.transform.position.xz();
            float distance = diff.magnitude;
            if (distance > walkDistance)
                run = true;
            if (distance > stopDistance)
                input = diff.normalized;
        }

        InputAI.SetAxis("horizontal", input.x);
        InputAI.SetAxis("vertical", input.y);
        InputAI.SetToggle("crossBtn", run);
    }
}
