using System;
using UnityEngine;

public class CeilingLightController : MonoBehaviour
{
    public enum EmissionFlickerCombineMode { Max, Average, Pair0Only }

    [Header("Auto wiring")]
    public bool autoFindPairsOnAwake = true;
    public bool autoFindEmissiveOnAwake = false;

    [Header("Children used by this ceiling")]
    public LightPairController[] pairs;
    public Renderer[] emissiveRenderers;
    public string emissionColorProperty = "_EmissionColor";

    [Header("Default pair settings (applied to every child LightPair)")]
    public bool applyPairDefaultsOnAwake = true;

    public Color defaultPrimarySpotColor = new Color32(229, 255, 184, 255);   // E5FFB8
    public float defaultPrimarySpotIntensity = 4f;

    public Color defaultPrimaryPointColor = new Color32(199, 213, 147, 255);  // C7D593
    public float defaultPrimaryPointIntensity = 1f;

    public Color defaultSecondarySpotColor = Color.red;
    public float defaultSecondarySpotIntensity = 10f;

    public Color defaultSecondaryPointColor = Color.red;
    public float defaultSecondaryPointIntensity = 1f;

    [Header("Primary / Secondary emission")]
    public Color primaryEmissionColor = Color.white;
    public float primaryEmissionMultiplier = 2f;
    public Color secondaryEmissionColor = Color.red;
    public float secondaryEmissionMultiplier = 2f;

    [Header("Emission reacts to flicker")]
    public bool emissionMirrorsFlicker = true;
    public EmissionFlickerCombineMode emissionFlickerMode = EmissionFlickerCombineMode.Max;

    [Header("Special modes")]
    public bool treatAsFloor = false;
    public bool forceEmissionOff = false;

    [Header("Start state")]
    public bool startOn = true;
    public bool startUseSecondary = false;
    public bool startAllPairsFlicker = false;

    bool _baseOn;
    bool _baseUseSecondary;
    bool _groupSuppressed;
    bool _proximitySuppressed;

    bool _surgeActive;
    bool _surgeUseSecondary;
    bool _surgeOverrideParams;
    Vector2 _surgeInterval = new Vector2(0.02f, 0.08f);
    Vector2 _surgeRange = new Vector2(0.05f, 1.35f);

    bool _emissionSecondaryOverrideActive;
    bool _emissionSecondaryOverrideValue;

    public bool IsOn { get; private set; }
    public bool UsingSecondary { get; private set; }
    public bool IsSurging => _surgeActive;
    public int PairCount => pairs == null ? 0 : pairs.Length;

    MaterialPropertyBlock _mpb;
    float[] _pairMult;
    Action<float>[] _handlers;

    void Awake()
    {
        if (autoFindPairsOnAwake)
            RefreshPairs();

        if (autoFindEmissiveOnAwake)
            RefreshEmissiveRenderers();

        if (applyPairDefaultsOnAwake)
            ApplyPairDefaultsToChildren();

        _baseOn = startOn;
        _baseUseSecondary = startUseSecondary;

        BuildFlickerHooks();
        RecomputeAndApply();

        if (startAllPairsFlicker)
            SetAllPairsFlicker(true);
    }

    void OnDestroy()
    {
        UnhookPairs();
    }

    [ContextMenu("Refresh Child LightPairs")]
    public void RefreshPairs()
    {
        pairs = GetComponentsInChildren<LightPairController>(true);
    }

    [ContextMenu("Refresh Child Renderers (emissive)")]
    public void RefreshEmissiveRenderers()
    {
        emissiveRenderers = GetComponentsInChildren<Renderer>(true);
    }

    [ContextMenu("Apply Pair Defaults To Children")]
    public void ApplyPairDefaultsToChildren()
    {
        if (pairs == null || pairs.Length == 0)
            RefreshPairs();

        if (pairs == null) return;

        foreach (var pair in pairs)
        {
            if (!pair) continue;

            pair.SetPrimaryColors(defaultPrimarySpotColor, defaultPrimaryPointColor);
            pair.SetPrimaryIntensities(defaultPrimarySpotIntensity, defaultPrimaryPointIntensity);

            pair.SetSecondaryColors(defaultSecondarySpotColor, defaultSecondaryPointColor);
            pair.SetSecondaryIntensities(defaultSecondarySpotIntensity, defaultSecondaryPointIntensity);
        }
    }

    public void SetOn(bool on) => SetBaseOn(on);
    public void SetUseSecondary(bool useSecondary) => SetBaseUseSecondary(useSecondary);
    public void SetFlicker(bool flicker) => SetAllPairsFlicker(flicker);

    public void SetPairFlicker(int pairIndex, bool flicker)
    {
        if (pairs == null || pairIndex < 0 || pairIndex >= pairs.Length) return;
        if (!pairs[pairIndex]) return;

        if (flicker) SetBaseOn(true);
        pairs[pairIndex].SetBaseFlicker(flicker);
        ApplyEmission();
    }

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

    public void SetAllPairsFlicker(bool flicker)
    {
        if (pairs == null) return;

        foreach (var p in pairs)
        {
            if (!p) continue;
            p.SetBaseFlicker(flicker);
        }

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
        {
            foreach (var p in pairs)
            {
                if (!p) continue;
                p.BeginSurge(useSecondary, overrideParams, _surgeInterval, _surgeRange);
            }
        }

        RecomputeAndApply();
    }

    public void EndSurge()
    {
        _surgeActive = false;
        _surgeOverrideParams = false;

        if (pairs != null)
        {
            foreach (var p in pairs)
            {
                if (!p) continue;
                p.EndSurge();
            }
        }

        RecomputeAndApply();
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
        if (pairs == null || pairIndex < 0 || pairIndex >= pairs.Length) return;
        if (!pairs[pairIndex]) return;

        pairs[pairIndex].SetProximitySuppressed(suppressed);
        ApplyEmission();
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

    public void SetForceEmissionOff(bool off)
    {
        forceEmissionOff = off;
        ApplyEmission();
    }

    public void SetEmissionUseSecondaryOverride(bool useSecondary)
    {
        _emissionSecondaryOverrideActive = true;
        _emissionSecondaryOverrideValue = useSecondary;
        ApplyEmission();
    }

    public void ClearEmissionUseSecondaryOverride()
    {
        _emissionSecondaryOverrideActive = false;
        ApplyEmission();
    }

    void BuildFlickerHooks()
    {
        UnhookPairs();

        _pairMult = new float[pairs != null ? pairs.Length : 0];
        _handlers = new Action<float>[pairs != null ? pairs.Length : 0];

        for (int i = 0; i < _pairMult.Length; i++)
            _pairMult[i] = 1f;

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

                if (emissionMirrorsFlicker)
                    ApplyEmission();
            };

            p.OnFlickerSample += _handlers[i];
        }
    }

    void UnhookPairs()
    {
        if (pairs == null || _handlers == null) return;

        for (int i = 0; i < pairs.Length; i++)
        {
            if (pairs[i] != null && _handlers[i] != null)
                pairs[i].OnFlickerSample -= _handlers[i];
        }
    }

    void RecomputeAndApply()
    {
        bool suppressed = _groupSuppressed || _proximitySuppressed;
        bool desiredOn = _baseOn && !suppressed && !treatAsFloor;
        bool desiredSecondary = _surgeActive ? _surgeUseSecondary : _baseUseSecondary;

        IsOn = desiredOn;
        UsingSecondary = desiredSecondary;

        if (pairs != null)
        {
            foreach (var p in pairs)
            {
                if (!p) continue;

                p.SetGroupSuppressed(_groupSuppressed);
                p.SetProximitySuppressed(_proximitySuppressed);
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

        if (!IsOn || treatAsFloor || forceEmissionOff)
        {
            SetEmission(Color.black);
            return;
        }

        bool emissionUsesSecondary = _emissionSecondaryOverrideActive
            ? _emissionSecondaryOverrideValue
            : UsingSecondary;

        Color baseColor = emissionUsesSecondary ? secondaryEmissionColor : primaryEmissionColor;
        float baseMult = emissionUsesSecondary ? secondaryEmissionMultiplier : primaryEmissionMultiplier;
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
            {
                float sum = 0f;
                for (int i = 0; i < _pairMult.Length; i++) sum += _pairMult[i];
                return Mathf.Max(0f, sum / _pairMult.Length);
            }

            case EmissionFlickerCombineMode.Max:
            default:
            {
                float max = 0f;
                for (int i = 0; i < _pairMult.Length; i++) max = Mathf.Max(max, _pairMult[i]);
                return Mathf.Max(0f, max);
            }
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