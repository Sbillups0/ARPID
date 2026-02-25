using System.Collections;
using UnityEngine;

public class LightFixtureController : MonoBehaviour
{
    [Header("Lights to control")]
    public Light[] lights;

    [Header("Optional: emissive panel renderers")]
    public Renderer[] emissiveRenderers;

    [Header("Shader emission property name (common: _EmissionColor)")]
    public string emissionColorProperty = "_EmissionColor";

    [Header("Emission colors")]
    public Color emissionOn = Color.white;
    public Color emissionOff = Color.black;
    public float emissionMultiplier = 2f;

    [Header("Flicker")]
    public bool flicker;
    public float flickerMinIntensity = 0.2f;
    public float flickerMaxIntensity = 1.2f;
    public Vector2 flickerInterval = new Vector2(0.03f, 0.12f);

    MaterialPropertyBlock _mpb;
    Coroutine _flickerRoutine;
    float[] _baseIntensities;

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        _baseIntensities = new float[lights != null ? lights.Length : 0];
        for (int i = 0; i < _baseIntensities.Length; i++)
            if (lights[i]) _baseIntensities[i] = lights[i].intensity;
    }

    void Start()
    {
        bool anyOn = AnyLightOn();
        SetEmission(anyOn);

        if (flicker) SetFlicker(true);
    }

    bool AnyLightOn()
    {
        if (lights == null) return false;
        foreach (var l in lights)
            if (l && l.enabled) return true;
        return false;
    }

    public void SetOn(bool on)
    {
        if (lights != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (!l) continue;

                l.enabled = on;
                if (on && i < _baseIntensities.Length)
                    l.intensity = _baseIntensities[i];
            }
        }

        SetEmission(on);

        if (!on) SetFlicker(false);
    }

    public void Toggle()
    {
        SetOn(!AnyLightOn());
    }

    public void SetFlicker(bool enable)
    {
        flicker = enable;

        if (_flickerRoutine != null)
        {
            StopCoroutine(_flickerRoutine);
            _flickerRoutine = null;
        }

        if (enable)
            _flickerRoutine = StartCoroutine(FlickerLoop());
        else
        {
            // restore base intensity when flicker stops
            if (lights != null)
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i] && i < _baseIntensities.Length)
                        lights[i].intensity = _baseIntensities[i];

            SetEmission(AnyLightOn());
        }
    }

    IEnumerator FlickerLoop()
    {
        while (true)
        {
            if (lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    var l = lights[i];
                    if (!l || !l.enabled) continue;

                    float t = Random.Range(flickerMinIntensity, flickerMaxIntensity);
                    l.intensity = _baseIntensities[i] * t;
                }
            }

            // Flicker emission slightly too
            float e = Random.Range(0.3f, 1.0f);
            SetEmission(true, e);

            yield return new WaitForSeconds(Random.Range(flickerInterval.x, flickerInterval.y));
        }
    }

    void SetEmission(bool on, float extraScale = 1f)
    {
        if (emissiveRenderers == null) return;

        Color c = on ? (emissionOn * (emissionMultiplier * extraScale)) : emissionOff;

        foreach (var r in emissiveRenderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(emissionColorProperty, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}