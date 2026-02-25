using UnityEngine;

public class RoomLightGroup : MonoBehaviour
{
    LightFixtureController[] fixtures;

    void Awake()
    {
        fixtures = GetComponentsInChildren<LightFixtureController>(true);
    }

    public void SetRoomOn(bool on)
    {
        foreach (var f in fixtures) f.SetOn(on);
    }

    public void SetRoomFlicker(bool flicker)
    {
        foreach (var f in fixtures) f.SetFlicker(flicker);
    }

    public void ToggleRoom()
    {
        if (fixtures.Length > 0) fixtures[0].Toggle(); // simple toggle approach
    }
}