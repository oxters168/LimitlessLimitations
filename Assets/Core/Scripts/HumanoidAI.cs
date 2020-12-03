using UnityEngine;
using UnityHelpers;

public class HumanoidAI : MonoBehaviour
{
    public Transform target;

    private IValueManager _inputAI;
    private IValueManager InputAI { get { if (_inputAI == null) _inputAI = GetComponent<IValueManager>(); return _inputAI; } }

    void Update()
    {
        Vector2 input = Vector2.zero;
        bool run = false;
        if (target != null)
        {
            Vector2 diff = target.position.xz() - transform.position.xz();
            float distance = diff.magnitude;
            if (distance > 1)
                run = true;
            if (distance > 0.01f)
                input = diff.normalized;
            // input = input.Sign();
        }

        // if (!input.IsZero())
        // {
            InputAI.SetAxis("horizontal", input.x);
            InputAI.SetAxis("vertical", input.y);
            InputAI.SetToggle("crossBtn", run);
        // }
    }
}
