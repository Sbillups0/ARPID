using UnityEngine;

public class MannequinBloodTarget : MonoBehaviour
{
    [Header("Assign all renderers that should change")]
    public Renderer[] renderersToChange;

    [Header("Materials")]
    public Material cleanMaterial;
    public Material bloodyMaterial;

    private bool isBloody = false;

    private void Awake()
    {
        // If you forget to assign renderers, grab all child renderers automatically
        if (renderersToChange == null || renderersToChange.Length == 0)
        {
            renderersToChange = GetComponentsInChildren<Renderer>();
        }

        SetClean();
    }

    public void SetBloody()
    {
        if (isBloody) return;
        isBloody = true;

        foreach (Renderer rend in renderersToChange)
        {
            if (rend != null)
            {
                rend.material = bloodyMaterial;
            }
        }
    }

    public void SetClean()
    {
        isBloody = false;

        foreach (Renderer rend in renderersToChange)
        {
            if (rend != null)
            {
                rend.material = cleanMaterial;
            }
        }
    }
}