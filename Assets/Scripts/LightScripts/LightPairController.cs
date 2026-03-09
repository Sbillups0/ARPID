using System;
using System.Collections;
using UnityEngine;

public class LightPairController : MonoBehaviour
{
    [Header("Assign the two lights in this pair")]
    public Light spotLight;
    public Light pointLight;

    [Header("Primary look (defaults match your current prefab settings)")]
    public Color primarySpotColor = new Color32(229, 255, 184, 255);   // E5FFB8
    public float primarySpotIntensity = 4f;

    public Color primaryPointColor = new Color32(199, 213, 147, 255);  // C7D593
    public float primaryPointIntensity = 1f;

    [Header("Secondary look")]
    public Color secondarySpotColor = Color.red;
    public float secondarySpotIntensity = 3f;

    public Color secondaryPointColor = Color.red;
    public float secondaryPointIntensity = 1f;

    [Header("Flicker settings")]
    public Vector2 flickerInterval = new Vector2(0.04f, 0.12f);
    public Vector2 flickerMultiplierRange = new Vector2(0.35f, 1.15f);

    [Header("Start state")]
    public bool startOn = true;
    public bool startUseSecondary = false;
    public bool startFlicker = false;

    public bool IsOn { get; private set; }
    public bool UsingSecondary { get; private set; }
    public bool IsFlickering { get; private set; }

    bool _baseOn;
    bool _baseUseSecondary;
    bool _baseFlicker;

    bool _groupSuppressed;
    bool _proximitySuppressed;

    bool _surgeActive;
    bool _surgeUseSecondary;
    bool _surgeOverrideParams;
    Vector2 _surgeInterval = new Vector2(0.02f, 0.08f);
    Vector2 _surgeRange = new Vector2(0.05f, 1.35f);

    Coroutine _flickerRoutine;

    public event Action<float> OnFlickerSample;

    void Awake()
    {
        _baseOn = startOn;
        _baseUseSecondary = startUseSecondary;
        _baseFlicker = startFlicker;

        RecomputeAndApply();
    }

    void OnDisable()
    {
        StopFlicker();
        OnFlickerSample?.Invoke(0f);
    }

    // ---------------- Base controls ----------------

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

    // ---------------- Suppression channels ----------------

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

    // ---------------- Surge API ----------------

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

    // ---------------- Look tuning ----------------

    public void SetPrimaryColors(Color spot, Color point)
    {
        primarySpotColor = spot;
        primaryPointColor = point;
        RecomputeAndApply();
    }

    public void SetSecondaryColors(Color spot, Color point)
    {
        secondarySpotColor = spot;
        secondaryPointColor = point;
        RecomputeAndApply();
    }

    public void SetPrimaryIntensities(float spotIntensity, float pointIntensity)
    {
        primarySpotIntensity = spotIntensity;
        primaryPointIntensity = pointIntensity;
        RecomputeAndApply();
    }

    public void SetSecondaryIntensities(float spotIntensity, float pointIntensity)
    {
        secondarySpotIntensity = spotIntensity;
        secondaryPointIntensity = pointIntensity;
        RecomputeAndApply();
    }

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
            if (spotLight)
            {
                spotLight.color = primarySpotColor;
                spotLight.intensity = primarySpotIntensity;
            }

            if (pointLight)
            {
                pointLight.color = primaryPointColor;
                pointLight.intensity = primaryPointIntensity;
            }
        }
        else
        {
            if (spotLight)
            {
                spotLight.color = secondarySpotColor;
                spotLight.intensity = secondarySpotIntensity;
            }

            if (pointLight)
            {
                pointLight.color = secondaryPointColor;
                pointLight.intensity = secondaryPointIntensity;
            }
        }
    }

    Vector2 CurrentInterval()
    {
        return (_surgeActive && _surgeOverrideParams) ? _surgeInterval : flickerInterval;
    }

    Vector2 CurrentRange()
    {
        return (_surgeActive && _surgeOverrideParams) ? _surgeRange : flickerMultiplierRange;
    }

    void StartFlicker()
    {
        if (_flickerRoutine == null)
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
            Vector2 range = CurrentRange();
            Vector2 interval = CurrentInterval();

            float mult = UnityEngine.Random.Range(range.x, range.y);

            if (!UsingSecondary)
            {
                if (spotLight)
                {
                    spotLight.color = primarySpotColor;
                    spotLight.intensity = primarySpotIntensity * mult;
                }

                if (pointLight)
                {
                    pointLight.color = primaryPointColor;
                    pointLight.intensity = primaryPointIntensity * mult;
                }
            }
            else
            {
                if (spotLight)
                {
                    spotLight.color = secondarySpotColor;
                    spotLight.intensity = secondarySpotIntensity * mult;
                }

                if (pointLight)
                {
                    pointLight.color = secondaryPointColor;
                    pointLight.intensity = secondaryPointIntensity * mult;
                }
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