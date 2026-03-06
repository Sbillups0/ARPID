using System;
using UnityEngine;

public class CeilingLightController : MonoBehaviour
{
    public enum EmissionFlickerCombineMode { Max, Average, Pair0Only }

    [Header("Auto wiring (recommended for variable ceiling sizes)")]
    public bool autoFindPairsOnAwake = true;
    public bool autoFindEmissiveOnAwake = false; // Only enable if you want ALL child Renderers (usually too broad)

    [Header("Light pairs (LightPair0, LightPair1, ...)")]
    public LightPairController[] pairs;

    [Header("Emission renderers (panel mesh renderers only)")]
    public Renderer[] emissiveRenderers;
    public string emissionColorProperty = "_EmissionColor";

    [Header("Primary / Secondary emission")]
    public Color primaryEmissionColor = Color.white;
    public float primaryEmissionMultiplier = 2f;
    public Color secondaryEmissionColor = Color.red;
    public float secondaryEmissionMultiplier = 2f;

    [Header("Emission reacts to flicker")]
    public bool emissionMirrorsFlicker = true;
    public EmissionFlickerCombineMode emissionFlickerMode = EmissionFlickerCombineMode.Max;

    [Header("Reuse as Floor / Force emission off")]
    public bool treatAsFloor = false;      // if true: lights forced OFF and emission OFF
    public bool forceEmissionOff = false;  // if true: emission forced OFF (lights can still be on)

    [Header("Start State")]
    public bool startOn = true;
    public bool startUseSecondary = false;

    // Base state (normal)
    bool _baseOn;
    bool _baseUseSecondary;
    bool _baseFlicker; // global flicker flag (optional)

    // Suppression channels (future group + proximity)
    bool _groupSuppressed;
    bool _proximitySuppressed;

    // Surge override for this ceiling
    bool _surgeActive;
    bool _surgeUseSecondary;
    bool _surgeOverrideParams;
    Vector2 _surgeInterval = new Vector2(0.02f, 0.08f);
    Vector2 _surgeRange = new Vector2(0.05f, 1.35f);

    public bool IsOn { get; private set; }
    public bool UsingSecondary { get; private set; }
    public bool IsSurging => _surgeActive;

    MaterialPropertyBlock _mpb;

    float[] _pairMult;
    Action<float>[] _handlers;

    void Awake()
    {
        if (autoFindPairsOnAwake)
            pairs = GetComponentsInChildren<LightPairController>(true);

        if (autoFindEmissiveOnAwake)
            emissiveRenderers = GetComponentsInChildren<Renderer>(true);

        _baseOn = startOn;
        _baseUseSecondary = startUseSecondary;
        _baseFlicker = false;

        BuildFlickerHooks();
        RecomputeAndApply();
    }

    void OnDestroy()
    {
        UnhookPairs();
    }

    // --------------------------------------------------------------------
    // Backwards-compatible API (so your existing scripts compile)
    // --------------------------------------------------------------------
    public void SetOn(bool on) => SetBaseOn(on);
    public void SetUseSecondary(bool useSecondary) => SetBaseUseSecondary(useSecondary);
    public void SetFlicker(bool flicker) => SetBaseFlicker(flicker);

    /// <summary>
    /// Old API: flicker ONE pair. This works with your RandomLightPairFlicker and Surge scripts.
    /// </summary>
    public void SetPairFlicker(int pairIndex, bool flicker)
    {
        if (pairs == null || pairIndex < 0 || pairIndex >= pairs.Length) return;
        if (!pairs[pairIndex]) return;

        // Ensure ceiling is on (if it’s off, pair won’t be visible)
        if (flicker) SetBaseOn(true);

        pairs[pairIndex].SetBaseFlicker(flicker);
        ApplyEmission();
    }

    // --------------------------------------------------------------------
    // New GameManager-ready API (base, suppression, surge)
    // --------------------------------------------------------------------
    public void SetBaseOn(bool on)
    {
        _baseOn = on;
        RecomputeAndApply();
    }

    public void SetBaseUseSecondary(bool useSecondary)
    {
        _baseUseSecondary = useSecondary;
        RecomputeAndApply();
    }

    /// <summary>
    /// Global flicker for all pairs (optional). Per-pair flicker remains possible via SetPairFlicker.
    /// </summary>
    public void SetBaseFlicker(bool flickerAllPairs)
    {
        _baseFlicker = flickerAllPairs;

        if (pairs != null)
            foreach (var p in pairs)
                if (p) p.SetBaseFlicker(flickerAllPairs);

        ApplyEmission();
    }

    public void SetGroupSuppressed(bool suppressed)
    {
        _groupSuppressed = suppressed;
        RecomputeAndApply();
    }

    public void SetProximitySuppressed(bool suppressed)
    {
        _proximitySuppressed = suppressed;
        RecomputeAndApply();
    }

    public void SetPairProximitySuppressed(int pairIndex, bool suppressed)
    {
        if (pairs == null || pairIndex < 0 || pairIndex >= pairs.Length || !pairs[pairIndex]) return;
        pairs[pairIndex].SetProximitySuppressed(suppressed);
        ApplyEmission();
    }

    public void BeginSurge(bool useSecondary, bool overrideParams = false,
                           Vector2? surgeInterval = null, Vector2? surgeRange = null)
    {
        _surgeActive = true;
        _surgeUseSecondary = useSecondary;

        _surgeOverrideParams = overrideParams;
        if (overrideParams)
        {
            _surgeInterval = surgeInterval ?? _surgeInterval;
            _surgeRange = surgeRange ?? _surgeRange;
        }

        if (pairs != null)
            foreach (var p in pairs)
                if (p) p.BeginSurge(useSecondary, overrideParams, _surgeInterval, _surgeRange);

        RecomputeAndApply();
    }

    public void EndSurge()
    {
        _surgeActive = false;
        _surgeOverrideParams = false;

        if (pairs != null)
            foreach (var p in pairs)
                if (p) p.EndSurge();

        RecomputeAndApply();
    }

    public void SetPrimaryEmission(Color color, float multiplier)
    {
        primaryEmissionColor = color;
        primaryEmissionMultiplier = multiplier;
        ApplyEmission();
    }

    public void SetSecondaryEmission(Color color, float multiplier)
    {
        secondaryEmissionColor = color;
        secondaryEmissionMultiplier = multiplier;
        ApplyEmission();
    }

    // --------------------------------------------------------------------
    // Internals
    // --------------------------------------------------------------------
    void BuildFlickerHooks()
    {
        UnhookPairs();

        _pairMult = new float[pairs != null ? pairs.Length : 0];
        _handlers = new Action<float>[pairs != null ? pairs.Length : 0];
        for (int i = 0; i < _pairMult.Length; i++) _pairMult[i] = 1f;

        HookPairs();
    }

    void HookPairs()
    {
        if (pairs == null) return;

        for (int i = 0; i < pairs.Length; i++)
        {
            int idx = i;
            var p = pairs[i];
            if (!p) continue;

            _handlers[i] = (mult) =>
            {
                if (_pairMult != null && idx < _pairMult.Length)
                    _pairMult[idx] = mult;

                if (emissionMirrorsFlicker) ApplyEmission();
            };

            p.OnFlickerSample += _handlers[i];
        }
    }

    void UnhookPairs()
    {
        if (pairs == null || _handlers == null) return;
        for (int i = 0; i < pairs.Length; i++)
            if (pairs[i] != null && _handlers[i] != null)
                pairs[i].OnFlickerSample -= _handlers[i];
    }

    void RecomputeAndApply()
    {
        bool suppressed = _groupSuppressed || _proximitySuppressed;

        bool desiredOn = _baseOn && !suppressed && !treatAsFloor;
        bool desiredSecondary = _surgeActive ? _surgeUseSecondary : _baseUseSecondary;

        IsOn = desiredOn;
        UsingSecondary = desiredSecondary;

        // Drive pairs base state (do NOT overwrite per-pair flicker here)
        if (pairs != null)
        {
            foreach (var p in pairs)
            {
                if (!p) continue;

                p.SetGroupSuppressed(_groupSuppressed);
                p.SetProximitySuppressed(_proximitySuppressed);

                // Base state set every time is fine
                p.SetBaseUseSecondary(_baseUseSecondary);
                p.SetBaseOn(IsOn);
            }
        }

        ApplyEmission();
    }

    void ApplyEmission()
    {
        if (emissiveRenderers == null || emissiveRenderers.Length == 0) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Off / floor / forced off => no emission
        if (!IsOn || treatAsFloor || forceEmissionOff)
        {
            SetEmission(Color.black);
            return;
        }

        Color baseColor = UsingSecondary ? secondaryEmissionColor : primaryEmissionColor;
        float baseMult = UsingSecondary ? secondaryEmissionMultiplier : primaryEmissionMultiplier;

        float flickerMult = emissionMirrorsFlicker ? CombinePairMultipliers() : 1f;
        SetEmission(baseColor * (baseMult * flickerMult));
    }

    float CombinePairMultipliers()
    {
        if (_pairMult == null || _pairMult.Length == 0) return 1f;

        switch (emissionFlickerMode)
        {
            case EmissionFlickerCombineMode.Pair0Only:
                return Mathf.Max(0f, _pairMult[0]);

            case EmissionFlickerCombineMode.Average:
                float sum = 0f;
                for (int i = 0; i < _pairMult.Length; i++) sum += _pairMult[i];
                return Mathf.Max(0f, sum / _pairMult.Length);

            case EmissionFlickerCombineMode.Max:
            default:
                float m = 0f;
                for (int i = 0; i < _pairMult.Length; i++) m = Mathf.Max(m, _pairMult[i]);
                return Mathf.Max(0f, m);
        }
    }

    void SetEmission(Color finalColor)
    {
        _mpb.Clear();
        _mpb.SetColor(emissionColorProperty, finalColor);

        foreach (var r in emissiveRenderers)
        {
            if (!r) continue;
            r.SetPropertyBlock(_mpb);
        }
    }
}