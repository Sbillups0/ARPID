using System.Collections;
using UnityEngine;

public class RoomSurgeFlicker : MonoBehaviour
{
    [Header("Assign your RoomCeilingController (or leave blank to auto-find)")]
    public RoomCeilingController room;

    [Header("Surge timing")]
    public float surgeOnSeconds = 3f;
    public float surgeOffSeconds = 6f;

    [Header("Start immediately")]
    public bool runOnStart = true;

    void Awake()
    {
        if (!room)
            room = FindFirstObjectByType<RoomCeilingController>();
    }

    void Start()
    {
        if (runOnStart)
            StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            SetAllPairsFlicker(true);
            yield return new WaitForSeconds(surgeOnSeconds);

            SetAllPairsFlicker(false);
            yield return new WaitForSeconds(surgeOffSeconds);
        }
    }

    void SetAllPairsFlicker(bool flicker)
    {
        if (!room) return;

        // Find every ceiling under the room
        var ceilings = room.GetComponentsInChildren<CeilingLightController>(room.includeInactive);

        foreach (var c in ceilings)
        {
            if (!c || c.pairs == null) continue;

            // Make sure the ceiling is on during flicker
            if (flicker) c.SetOn(true);

            for (int i = 0; i < c.pairs.Length; i++)
            {
                c.SetPairFlicker(i, flicker);
            }
        }
    }

    // Optional: manual trigger from Inspector context menu
    [ContextMenu("Surge ON Now")]
    public void SurgeOnNow() => SetAllPairsFlicker(true);

    [ContextMenu("Surge OFF Now")]
    public void SurgeOffNow() => SetAllPairsFlicker(false);
}