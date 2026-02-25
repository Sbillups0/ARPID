using System;
using UnityEngine;

public class CeilingLightController : MonoBehaviour
{
    public enum EmissionFlickerCombineMode { Max, Average, Pair0Only }

    [Header("Light pairs (LightPair0, LightPair1, ...)")]
    public LightPairController[] pairs;

    [Header("Emission renderers (ONE attachment on the ceiling)")]
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

    public bool IsOn { get; private set; }
    public bool UsingSecondary { get; private set; }

    MaterialPropertyBlock _mpb;

    float[] _pairMult;
    Action<float>[] _handlers;

    void Awake()
    {
        IsOn = startOn;
        UsingSecondary = startUseSecondary;

        _pairMult = new float[pairs != null ? pairs.Length : 0];
        _handlers = new Action<float>[pairs != null ? pairs.Length : 0];
        for (int i = 0; i < _pairMult.Length; i++) _pairMult[i] = 1f;

        HookPairs();
        ApplyAll();
    }

    void OnDestroy()
    {
        UnhookPairs();
    }

    // ---------------- Public API ----------------

    public void SetOn(bool on)
    {
        if (treatAsFloor) on = false;

        IsOn = on;

        if (pairs != null)
            foreach (var p in pairs)
                if (p) p.SetOn(on);

        ApplyEmission();
    }

    public void SetUseSecondary(bool useSecondary)
    {
        UsingSecondary = useSecondary;

        if (pairs != null)
            foreach (var p in pairs)
                if (p) p.SetUseSecondary(useSecondary);

        ApplyEmission();
    }

    public void SetPairFlicker(int pairIndex, bool flicker)
    {
        if (pairs == null || pairIndex < 0 || pairIndex >= pairs.Length) return;
        if (!pairs[pairIndex]) return;

        pairs[pairIndex].SetFlicker(flicker);

        if (!flicker && _pairMult != null && pairIndex < _pairMult.Length)
            _pairMult[pairIndex] = 1f;

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

    // ---------------- Internals ----------------

    void ApplyAll()
    {
        if (pairs != null)
        {
            foreach (var p in pairs)
            {
                if (!p) continue;
                p.SetUseSecondary(UsingSecondary);
                p.SetOn(IsOn);
            }
        }
        ApplyEmission();
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

    void ApplyEmission()
    {
        if (emissiveRenderers == null || emissiveRenderers.Length == 0) return;

        // Floor mode or forced off => no glow
        if (treatAsFloor || forceEmissionOff || !IsOn)
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
        if (emissiveRenderers == null || emissiveRenderers.Length == 0) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        _mpb.Clear();
        _mpb.SetColor(emissionColorProperty, finalColor);

        foreach (var r in emissiveRenderers)
        {
            if (!r) continue;
            r.SetPropertyBlock(_mpb);
        }
    }
}