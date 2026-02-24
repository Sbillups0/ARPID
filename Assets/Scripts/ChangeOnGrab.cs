using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class ChangeOnGrab : MonoBehaviour
{
    [Header("Colors")]
    public Color releasedColor = Color.white;
    public Color grabbedColor = Color.green;

    Renderer _renderer;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Use .material to get an instance for THIS object (so you don't recolor a shared material asset).
        SetColor(releasedColor);
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnSelectEntered);
        _grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnSelectEntered);
        _grab.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args) => SetColor(grabbedColor);
    void OnSelectExited(SelectExitEventArgs args) => SetColor(releasedColor);

    void SetColor(Color c)
    {
        if (_renderer != null)
            _renderer.material.color = c;
    }
}
