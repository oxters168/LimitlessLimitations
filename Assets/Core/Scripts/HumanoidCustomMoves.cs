using UnityEngine;
using UnityHelpers;

public class HumanoidCustomMoves : MonoBehaviour
{
    private AnimateAndMoveCharacter _character;
    private AnimateAndMoveCharacter Character { get { if (_character == null) _character = GetComponent<AnimateAndMoveCharacter>(); return _character; } }

    public CustomAnimationData rollData;

    private bool prevRoll;

    void Update()
    {
        if (!prevRoll && Character.GetToggle("squareBtn") && !Character.IsUnderwater)
        {
            prevRoll = true;
            Character.RunCustomAnim(rollData);
        }
        else
            prevRoll = false;
    }
}
