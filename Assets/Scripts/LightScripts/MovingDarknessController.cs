using System;
using System.Collections.Generic;
using UnityEngine;

public class MovingDarknessController : MonoBehaviour
{
    public enum ZoneEffectMode
    {
        None,
        FlickerOverride,
        SurgeOverride
    }

    [Serializable]
    public class DarknessZone
    {
        public string name = "Zone";
        public bool enabled = true;
        public float radius = 10f;

        [Header("Color / off")]
        public bool useSecondary = false;
        public bool turnOffLights = false;

        [Header("Override behavior")]
        public ZoneEffectMode effectMode = ZoneEffectMode.None;
        public Vector2 overrideInterval = new Vector2(0.04f, 0.12f);
        public Vector2 overrideRange = new Vector2(0.35f, 1.15f);
    }

    [Header("General")]
    public bool controllerActive = true;
    public bool playOnStart = true;
    public bool loopPath = true;

    [Header("Trajectory")]
    public Transform[] trajectoryPoints;
    [Tooltip("Element i = time for segment starting at point i.")]
    public float[] segmentTimes;
    public bool useSameTimeForAllSegments = false;
    public float sameSegmentTime = 30f;

    [Header("Objects to affect")]
    [Tooltip("Drag whole ceiling parents, single CeilingLightController objects, or LightPair objects/parents here.")]
    public Transform[] affectObjects;
    public CeilingLightController[] directCeilings;
    public LightPairController[] directPairs;
    public bool resolveTargetsOnStart = true;

    [Header("Zones (smallest matching radius wins)")]
    public DarknessZone[] zones = new DarknessZone[3];

    [Header("Visual shell")]
    public GameObject darknessVisualRoot;

    readonly List<CeilingLightController> _resolvedCeilings = new List<CeilingLightController>();
    readonly List<LightPairController> _resolvedPairs = new List<LightPairController>();
    readonly Dictionary<LightPairController, CeilingLightController> _pairToCeiling = new Dictionary<LightPairController, CeilingLightController>();

    int _segmentIndex;
    float _segmentElapsed;
    bool _moving;

    Vector3 _segmentStart;
    Vector3 _segmentEnd;
    float _segmentDuration;

    void Reset()
    {
        zones = new DarknessZone[3];

        zones[0] = new DarknessZone
        {
            name = "Outer Secondary Zone",
            enabled = true,
            radius = 23f,
            useSecondary = true,
            turnOffLights = false,
            effectMode = ZoneEffectMode.None
        };

        zones[1] = new DarknessZone
        {
            name = "Flicker Override Zone",
            enabled = true,
            radius = 19f,
            useSecondary = true,
            turnOffLights = false,
            effectMode = ZoneEffectMode.FlickerOverride,
            overrideInterval = new Vector2(0.03f, 0.06f),
            overrideRange = new Vector2(0.08f, 1.10f)
        };

        zones[2] = new DarknessZone
        {
            name = "Blackout Zone",
            enabled = true,
            radius = 15f,
            useSecondary = true,
            turnOffLights = true,
            effectMode = ZoneEffectMode.None
        };
    }

    void Awake()
    {
        if (resolveTargetsOnStart)
            RefreshTargets();
    }

    void Start()
    {
        if (darknessVisualRoot != null)
            darknessVisualRoot.SetActive(controllerActive);

        if (playOnStart && controllerActive)
            BeginPath();
    }

    void Update()
    {
        if (!controllerActive) return;

        if (_moving)
            UpdateMovement();

        ApplyDarkness();
    }

    public void SetControllerActive(bool active)
    {
        controllerActive = active;

        if (darknessVisualRoot != null)
            darknessVisualRoot.SetActive(active);

        if (!active)
            ClearDarknessEffects();

        if (active && !_moving && playOnStart)
            BeginPath();
    }

    [ContextMenu("Refresh Targets")]
    public void RefreshTargets()
    {
        _resolvedCeilings.Clear();
        _resolvedPairs.Clear();
        _pairToCeiling.Clear();

        HashSet<CeilingLightController> ceilingSet = new HashSet<CeilingLightController>();
        HashSet<LightPairController> pairSet = new HashSet<LightPairController>();

        if (affectObjects != null)
        {
            foreach (var obj in affectObjects)
            {
                if (!obj) continue;

                var ceilings = obj.GetComponentsInChildren<CeilingLightController>(true);
                foreach (var c in ceilings)
                {
                    if (c) ceilingSet.Add(c);
                }

                var pairs = obj.GetComponentsInChildren<LightPairController>(true);
                foreach (var p in pairs)
                {
                    if (p) pairSet.Add(p);
                }
            }
        }

        if (directCeilings != null)
        {
            foreach (var c in directCeilings)
            {
                if (c) ceilingSet.Add(c);
            }
        }

        if (directPairs != null)
        {
            foreach (var p in directPairs)
            {
                if (p) pairSet.Add(p);
            }
        }

        foreach (var c in ceilingSet)
            _resolvedCeilings.Add(c);

        foreach (var p in pairSet)
        {
            if (!p) continue;

            _resolvedPairs.Add(p);

            var parentCeiling = p.GetComponentInParent<CeilingLightController>();
            if (parentCeiling != null && !_pairToCeiling.ContainsKey(p))
                _pairToCeiling.Add(p, parentCeiling);
        }

        // Ensure all child pairs of direct ceilings are included
        foreach (var c in _resolvedCeilings)
        {
            if (!c || c.pairs == null) continue;

            foreach (var p in c.pairs)
            {
                if (!p) continue;

                if (!_resolvedPairs.Contains(p))
                    _resolvedPairs.Add(p);

                if (!_pairToCeiling.ContainsKey(p))
                    _pairToCeiling.Add(p, c);
            }
        }
    }

    [ContextMenu("Begin Path")]
    public void BeginPath()
    {
        if (trajectoryPoints == null || trajectoryPoints.Length < 2)
        {
            _moving = false;
            return;
        }

        _segmentIndex = 0;
        transform.position = trajectoryPoints[0].position;
        SetupSegment(_segmentIndex);
        _moving = true;
    }

    [ContextMenu("Stop Path")]
    public void StopPath()
    {
        _moving = false;
    }

    void SetupSegment(int startIndex)
    {
        int nextIndex = startIndex + 1;

        if (nextIndex >= trajectoryPoints.Length)
        {
            if (!loopPath)
            {
                _moving = false;
                transform.position = trajectoryPoints[trajectoryPoints.Length - 1].position;
                return;
            }

            nextIndex = 0;
        }

        _segmentStart = trajectoryPoints[startIndex].position;
        _segmentEnd = trajectoryPoints[nextIndex].position;
        _segmentElapsed = 0f;
        _segmentDuration = GetSegmentDuration(startIndex);
    }

    float GetSegmentDuration(int startIndex)
    {
        if (useSameTimeForAllSegments)
            return Mathf.Max(0.01f, sameSegmentTime);

        if (segmentTimes == null || startIndex < 0 || startIndex >= segmentTimes.Length)
            return Mathf.Max(0.01f, sameSegmentTime);

        return Mathf.Max(0.01f, segmentTimes[startIndex]);
    }

    void UpdateMovement()
    {
        _segmentElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_segmentElapsed / Mathf.Max(0.01f, _segmentDuration));

        transform.position = Vector3.Lerp(_segmentStart, _segmentEnd, t);

        if (t >= 1f)
        {
            _segmentIndex++;

            if (_segmentIndex >= trajectoryPoints.Length - 1)
            {
                if (loopPath)
                    _segmentIndex = 0;
                else
                {
                    _moving = false;
                    return;
                }
            }

            SetupSegment(_segmentIndex);
        }
    }

    DarknessZone GetBestZone(float distance)
    {
        DarknessZone best = null;
        float bestRadius = float.MaxValue;

        if (zones == null) return null;

        foreach (var zone in zones)
        {
            if (zone == null || !zone.enabled) continue;
            if (distance > zone.radius) continue;

            if (zone.radius < bestRadius)
            {
                best = zone;
                bestRadius = zone.radius;
            }
        }

        return best;
    }

    void ApplyDarkness()
    {
        Dictionary<CeilingLightController, bool> ceilingAnySecondary = new Dictionary<CeilingLightController, bool>();
        Dictionary<CeilingLightController, int> ceilingTotalPairs = new Dictionary<CeilingLightController, int>();
        Dictionary<CeilingLightController, int> ceilingOnPairs = new Dictionary<CeilingLightController, int>();

        foreach (var ceiling in _resolvedCeilings)
        {
            if (!ceiling) continue;
            ceilingAnySecondary[ceiling] = false;
            ceilingTotalPairs[ceiling] = 0;
            ceilingOnPairs[ceiling] = 0;
        }

        foreach (var pair in _resolvedPairs)
        {
            if (!pair) continue;

            float dist = Vector3.Distance(transform.position, pair.transform.position);
            DarknessZone zone = GetBestZone(dist);

            // Clear darkness-owned overrides first
            pair.EndSurge();
            pair.SetBaseFlicker(false);
            pair.SetProximitySuppressed(false);
            pair.SetBaseUseSecondary(false);

            if (zone != null)
            {
                if (zone.useSecondary)
                    pair.SetBaseUseSecondary(true);

                if (zone.turnOffLights)
                {
                    pair.SetProximitySuppressed(true);
                }
                else
                {
                    switch (zone.effectMode)
                    {
                        case ZoneEffectMode.FlickerOverride:
                            pair.BeginSurge(zone.useSecondary, true, zone.overrideInterval, zone.overrideRange);
                            break;

                        case ZoneEffectMode.SurgeOverride:
                            pair.BeginSurge(zone.useSecondary, true, zone.overrideInterval, zone.overrideRange);
                            break;
                    }
                }
            }

            if (_pairToCeiling.TryGetValue(pair, out var parentCeiling) && parentCeiling != null)
            {
                if (!ceilingTotalPairs.ContainsKey(parentCeiling))
                    ceilingTotalPairs[parentCeiling] = 0;

                if (!ceilingOnPairs.ContainsKey(parentCeiling))
                    ceilingOnPairs[parentCeiling] = 0;

                if (!ceilingAnySecondary.ContainsKey(parentCeiling))
                    ceilingAnySecondary[parentCeiling] = false;

                ceilingTotalPairs[parentCeiling]++;

                if (pair.IsOn)
                    ceilingOnPairs[parentCeiling]++;

                if (zone != null && zone.useSecondary)
                    ceilingAnySecondary[parentCeiling] = true;
            }
        }

        foreach (var ceiling in _resolvedCeilings)
        {
            if (!ceiling) continue;

            bool hasPairs = ceilingTotalPairs.ContainsKey(ceiling) && ceilingTotalPairs[ceiling] > 0;
            bool allPairsOff = hasPairs && ceilingOnPairs[ceiling] == 0;
            bool anySecondary = ceilingAnySecondary.ContainsKey(ceiling) && ceilingAnySecondary[ceiling];

            // NEW desired behavior:
            // emission only turns off if ALL pairs under this ceiling are off
            ceiling.SetForceEmissionOff(allPairsOff);

            if (anySecondary && !allPairsOff)
                ceiling.SetEmissionUseSecondaryOverride(true);
            else
                ceiling.ClearEmissionUseSecondaryOverride();
        }
    }

    void ClearDarknessEffects()
    {
        foreach (var pair in _resolvedPairs)
        {
            if (!pair) continue;
            pair.EndSurge();
            pair.SetBaseFlicker(false);
            pair.SetProximitySuppressed(false);
            pair.SetBaseUseSecondary(false);
        }

        foreach (var ceiling in _resolvedCeilings)
        {
            if (!ceiling) continue;
            ceiling.SetForceEmissionOff(false);
            ceiling.ClearEmissionUseSecondaryOverride();
        }
    }
}