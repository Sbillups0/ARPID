using UnityEngine;
using UnityEngine.InputSystem;

public class LightTestToggle : MonoBehaviour
{
    public LightFixtureController fixture;

    void Awake()
    {
        if (!fixture) fixture = GetComponent<LightFixtureController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || fixture == null) return;

        if (kb.tKey.wasPressedThisFrame) fixture.Toggle();
        if (kb.fKey.wasPressedThisFrame) fixture.SetFlicker(true);
        if (kb.gKey.wasPressedThisFrame) fixture.SetFlicker(false);
    }
}