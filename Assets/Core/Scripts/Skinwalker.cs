using UnityEngine;

public class Skinwalker : MonoBehaviour
{
    [Tooltip("Sets the shown shell")]
    public int shellIndex;
    private int prevShellIndex = -1;

    [Space(10), Tooltip("Sets the material of the shell from the custom materials array (-1 means original)")]
    public int materialIndex = -1;
    private int prevMaterialIndex = int.MinValue;

    private Animator _animator;
    public Animator CurrentAnimator { get { if (_animator == null || !_animator.gameObject.activeInHierarchy) _animator = GetComponentInChildren<Animator>(); return _animator; } }
    
    private AnimateAndMoveCharacter _character;
    private AnimateAndMoveCharacter Character { get { if (_character == null) _character = GetComponentInParent<AnimateAndMoveCharacter>(); return _character; } }
    
    public HumanoidShell CurrentShell { get { return shells[shellIndex]; } }

    [Space(10)]   
    public HumanoidShell[] shells;
    [Space(10)]
    public Material[] customMaterials;

    private Material[] originalMaterials;

    void Update()
    {
        RefreshChosenShell();
        RefreshShellMaterial();
    }
    void OnTriggerStay(Collider other)
    {
        Character.OnTriggerStay(other);
    }

    private void RefreshChosenShell()
    {
        if (shellIndex != prevShellIndex)
        {
            RevertShellMaterials();

            for (int i = 0; i < shells.Length; i++)
            {
                shells[i].gameObject.SetActive(i == shellIndex);
            }
            prevShellIndex = shellIndex;
        }
    }
    private void RefreshShellMaterial()
    {
        if (materialIndex != prevMaterialIndex)
        {
            RevertShellMaterials();
            prevMaterialIndex = materialIndex;
        }

        if (originalMaterials == null && materialIndex >= 0)
        {
            originalMaterials = CurrentShell.shellRenderer.materials;
            Material[] appliedMaterials = new Material[originalMaterials.Length];
            for (int i = 0; i < appliedMaterials.Length; i++)
                appliedMaterials[i] = customMaterials[materialIndex];
            CurrentShell.shellRenderer.materials = appliedMaterials;
        }
    }
    private void RevertShellMaterials()
    {
        if (originalMaterials != null)
        {
            CurrentShell.shellRenderer.materials = originalMaterials;
            originalMaterials = null;
        }
    }
}
