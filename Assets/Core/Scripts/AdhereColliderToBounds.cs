using UnityEngine;
using UnityHelpers;

public class AdhereColliderToBounds : MonoBehaviour
{
    public BoxCollider[] adherees;

    void Update()
    {
        if (adherees != null && adherees.Length > 0)
        {
            var bounds = transform.GetTotalBounds(Space.World);
            for (int i = 0; i < adherees.Length; i++)
            {
                adherees[i].center = transform.InverseTransformPoint(bounds.center);
                adherees[i].size = bounds.size;
            }
        }
    }
}
