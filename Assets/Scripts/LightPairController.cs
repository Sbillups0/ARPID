using System;
using System.Collections;
using UnityEngine;

public class LightPairController : MonoBehaviour
{
    [Header("Assign the two lights in this pair")]
    public Light spotLight;
    public Light pointLight;

    [Header("Primary look (THIS pair)")]
    public Color primarySpotColor = Color.white;
    public float primarySpotIntensity = 10f;
    public Color primaryPointColor = Color.white;
    public float primaryPointIntensity = 1f;

    [Header("Secondary look (THIS pair)")]
    public Color secondarySpotColor = Color.red;
    public float secondarySpotIntensity = 10f;
    public Color secondaryPointColor = Color.red;
    public float secondaryPointIntensity = 1f;

    [Header("Flicker settings (both lights flicker together)")]
    public Vector2 flickerInterval = new Vector2(0.04f, 0.12f);
    public Vector2 flickerMultiplierRange = new Vector2(0.35f, 1.15f);

    public bool IsOn { get; private set; } = true;
    public bool UsingSecondary { get; private set; } = false;
    public bool IsFlickering { get; private set; } = false;

    Coroutine _flickerRoutine;

    // Emits flicker multiplier samples so the ceiling can mirror on emission.
    public event Action<float> OnFlickerSample;

    void OnDisable()
    {
        StopFlicker();
        OnFlickerSample?.Invoke(0f);
    }

    // --- Called by ceiling/room/game manager ---

    public void SetOn(bool on)
    {
        IsOn = on;

        if (spotLight) spotLight.enabled = on;
        if (pointLight) pointLight.enabled = on;

        if (!on)
        {
            StopFlicker();
            OnFlickerSample?.Invoke(0f);
            return;
        }

        ApplyStableLook();
        if (IsFlickering) StartFlicker();
        else OnFlickerSample?.Invoke(1f);
    }

    public void SetUseSecondary(bool useSecondary)
    {
        UsingSecondary = useSecondary;
        if (!IsOn) return;

        ApplyStableLook();
        if (!IsFlickering) OnFlickerSample?.Invoke(1f);
    }

    public void SetFlicker(bool enable)
    {
        IsFlickering = enable;

        if (!IsOn)
        {
            StopFlicker();
            OnFlickerSample?.Invoke(0f);
            return;
        }

        if (enable) StartFlicker();
        else
        {
            StopFlicker();
            ApplyStableLook();
            OnFlickerSample?.Invoke(1f);
        }
    }

    // GameManager-friendly setters
    public void SetPrimaryColors(Color spot, Color point) { primarySpotColor = spot; primaryPointColor = point; if (IsOn && !UsingSecondary && !IsFlickering) ApplyStableLook(); }
    public void SetSecondaryColors(Color spot, Color point) { secondarySpotColor = spot; secondaryPointColor = point; if (IsOn && UsingSecondary && !IsFlickering) ApplyStableLook(); }
    public void SetPrimaryIntensities(float spotI, float pointI) { primarySpotIntensity = spotI; primaryPointIntensity = pointI; if (IsOn && !UsingSecondary && !IsFlickering) ApplyStableLook(); }
    public void SetSecondaryIntensities(float spotI, float pointI) { secondarySpotIntensity = spotI; secondaryPointIntensity = pointI; if (IsOn && UsingSecondary && !IsFlickering) ApplyStableLook(); }

    // --- Internals ---

    void ApplyStableLook()
    {
        if (!UsingSecondary)
        {
            if (spotLight) { spotLight.color = primarySpotColor; spotLight.intensity = primarySpotIntensity; }
            if (pointLight) { pointLight.color = primaryPointColor; pointLight.intensity = primaryPointIntensity; }
        }
        else
        {
            if (spotLight) { spotLight.color = secondarySpotColor; spotLight.intensity = secondarySpotIntensity; }
            if (pointLight) { pointLight.color = secondaryPointColor; pointLight.intensity = secondaryPointIntensity; }
        }
    }

    void StartFlicker()
    {
        StopFlicker();
        _flickerRoutine = StartCoroutine(FlickerLoop());
    }

    void StopFlicker()
    {
        if (_flickerRoutine != null)
        {
            StopCoroutine(_flickerRoutine);
            _flickerRoutine = null;
        }
    }

    IEnumerator FlickerLoop()
    {
        while (IsOn && IsFlickering)
        {
            float mult = UnityEngine.Random.Range(flickerMultiplierRange.x, flickerMultiplierRange.y);

            if (!UsingSecondary)
            {
                if (spotLight) { spotLight.color = primarySpotColor; spotLight.intensity = primarySpotIntensity * mult; }
                if (pointLight) { pointLight.color = primaryPointColor; pointLight.intensity = primaryPointIntensity * mult; }
            }
            else
            {
                if (spotLight) { spotLight.color = secondarySpotColor; spotLight.intensity = secondarySpotIntensity * mult; }
                if (pointLight) { pointLight.color = secondaryPointColor; pointLight.intensity = secondaryPointIntensity * mult; }
            }

            OnFlickerSample?.Invoke(mult);

            yield return new WaitForSeconds(UnityEngine.Random.Range(flickerInterval.x, flickerInterval.y));
        }

        ApplyStableLook();
        OnFlickerSample?.Invoke(IsOn ? 1f : 0f);
    }
}