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

    [Header("Flicker settings (normal)")]
    public Vector2 flickerInterval = new Vector2(0.04f, 0.12f);
    public Vector2 flickerMultiplierRange = new Vector2(0.35f, 1.15f);

    [Header("Start state")]
    public bool startOn = true;
    public bool startUseSecondary = false;

    // Effective state (what’s actually happening)
    public bool IsOn { get; private set; }
    public bool UsingSecondary { get; private set; }
    public bool IsFlickering { get; private set; }

    // Base state (what the ceiling/game wants normally)
    bool _baseOn;
    bool _baseUseSecondary;
    bool _baseFlicker;

    // Suppression channels for future logic
    bool _groupSuppressed;
    bool _proximitySuppressed;

    // Surge override
    bool _surgeActive;
    bool _surgeUseSecondary;
    Vector2 _surgeInterval;
    Vector2 _surgeRange;
    bool _surgeOverrideParams;

    Coroutine _flickerRoutine;

    // Ceiling listens to this to mirror emission flicker
    public event Action<float> OnFlickerSample;

    void Awake()
    {
        _baseOn = startOn;
        _baseUseSecondary = startUseSecondary;
        _baseFlicker = false;

        RecomputeAndApply();
    }

    void OnDisable()
    {
        StopFlicker();
        OnFlickerSample?.Invoke(0f);
    }

    // ---------------- Base controls (ceiling/game manager) ----------------

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

    public void SetBaseFlicker(bool flicker)
    {
        _baseFlicker = flicker;
        RecomputeAndApply();
    }

    // ---------------- Suppression channels (ready for group/proximity) ----------------

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

    // ---------------- Surge API (GameManager/ceiling calls this) ----------------

    /// <summary>
    /// Surge forces flicker ON, and optionally forces secondary colors.
    /// If overrideParams=true, uses the provided interval/range for the surge.
    /// </summary>
    public void BeginSurge(bool useSecondary, bool overrideParams = false,
                           Vector2 surgeInterval = default, Vector2 surgeRange = default)
    {
        _surgeActive = true;
        _surgeUseSecondary = useSecondary;

        _surgeOverrideParams = overrideParams;
        if (overrideParams)
        {
            _surgeInterval = (surgeInterval == default) ? new Vector2(0.02f, 0.08f) : surgeInterval;
            _surgeRange = (surgeRange == default) ? new Vector2(0.05f, 1.35f) : surgeRange;
        }

        RecomputeAndApply();
    }

    public void EndSurge()
    {
        _surgeActive = false;
        _surgeOverrideParams = false;
        RecomputeAndApply();
    }

    // ---------------- Pair tuning (RoomCeilingController uses these) ----------------

    public void SetPrimaryColors(Color spot, Color point) { primarySpotColor = spot; primaryPointColor = point; RecomputeAndApply(); }
    public void SetSecondaryColors(Color spot, Color point) { secondarySpotColor = spot; secondaryPointColor = point; RecomputeAndApply(); }
    public void SetPrimaryIntensities(float spotI, float pointI) { primarySpotIntensity = spotI; primaryPointIntensity = pointI; RecomputeAndApply(); }
    public void SetSecondaryIntensities(float spotI, float pointI) { secondarySpotIntensity = spotI; secondaryPointIntensity = pointI; RecomputeAndApply(); }

    // ---------------- Internals ----------------

    void RecomputeAndApply()
    {
        bool suppressed = _groupSuppressed || _proximitySuppressed;

        bool desiredOn = _baseOn && !suppressed;
        bool desiredSecondary = _surgeActive ? _surgeUseSecondary : _baseUseSecondary;
        bool desiredFlicker = _surgeActive || _baseFlicker;

        IsOn = desiredOn;
        UsingSecondary = desiredSecondary;
        IsFlickering = desiredFlicker;

        if (spotLight) spotLight.enabled = IsOn;
        if (pointLight) pointLight.enabled = IsOn;

        if (!IsOn)
        {
            StopFlicker();
            OnFlickerSample?.Invoke(0f);
            return;
        }

        if (IsFlickering)
            StartFlicker();
        else
        {
            StopFlicker();
            ApplyStableLook();
            OnFlickerSample?.Invoke(1f);
        }
    }

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

    Vector2 CurrentInterval()
    {
        if (_surgeActive && _surgeOverrideParams) return _surgeInterval;
        return flickerInterval;
    }

    Vector2 CurrentRange()
    {
        if (_surgeActive && _surgeOverrideParams) return _surgeRange;
        return flickerMultiplierRange;
    }

    void StartFlicker()
    {
        if (_flickerRoutine != null) return; // already running
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
            var range = CurrentRange();
            var interval = CurrentInterval();

            float mult = UnityEngine.Random.Range(range.x, range.y);

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
            yield return new WaitForSeconds(UnityEngine.Random.Range(interval.x, interval.y));
        }

        _flickerRoutine = null;

        if (IsOn)
        {
            ApplyStableLook();
            OnFlickerSample?.Invoke(1f);
        }
        else
        {
            OnFlickerSample?.Invoke(0f);
        }
    }
}