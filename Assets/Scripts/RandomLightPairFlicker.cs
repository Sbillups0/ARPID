using System.Collections;
using UnityEngine;

public class RandomLightPairFlicker : MonoBehaviour
{
    public RoomCeilingController room;

    [Header("Auto-run")]
    public bool runOnStart = true;

    [Header("Timing")]
    public Vector2 timeBetweenEvents = new Vector2(0.6f, 2.5f);
    public Vector2 flickerDuration = new Vector2(0.4f, 1.5f);

    [Header("How many flickers can overlap")]
    public int maxSimultaneousFlickers = 2;

    int _active;

    void Awake()
    {
        if (!room) room = GetComponent<RoomCeilingController>();
    }

    void Start()
    {
        if (runOnStart) StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            if (_active < maxSimultaneousFlickers)
                TryStartOne();

            yield return new WaitForSeconds(Random.Range(timeBetweenEvents.x, timeBetweenEvents.y));
        }
    }

    void TryStartOne()
    {
        if (!room) return;

        room.RefreshCache();
        var ceilings = GetComponentsInChildren<CeilingLightController>(room.includeInactive);
        if (ceilings == null || ceilings.Length == 0) return;

        CeilingLightController chosen = null;
        for (int tries = 0; tries < 12; tries++)
        {
            var c = ceilings[Random.Range(0, ceilings.Length)];
            if (c && c.pairs != null && c.pairs.Length > 0)
            {
                chosen = c;
                break;
            }
        }
        if (!chosen) return;

        int pairIndex = Random.Range(0, chosen.pairs.Length);
        if (chosen.pairs[pairIndex] == null) return;

        StartCoroutine(FlickerOnce(chosen, pairIndex));
    }

    IEnumerator FlickerOnce(CeilingLightController ceiling, int pairIndex)
    {
        _active++;

        ceiling.SetOn(true); // ensure it's on
        ceiling.SetPairFlicker(pairIndex, true);

        yield return new WaitForSeconds(Random.Range(flickerDuration.x, flickerDuration.y));

        ceiling.SetPairFlicker(pairIndex, false);

        _active--;
    }
}