using UnityEngine;

public class RoomCeilingController : MonoBehaviour
{
    [Header("Auto-find all CeilingLightController under this Room")]
    public bool includeInactive = true;

    [Header("Apply automatically when Play starts")]
    public bool applyOnStart = false;

    [Header("Room-wide overrides")]
    public bool treatAllAsFloor = false;
    public bool forceAllEmissionOff = false;

    [Header("Shadow control (fix URP shadow atlas spam)")]
    public bool pointLightsCastShadows = false;
    public bool spotLightsCastShadows = false;

    [Tooltip("0 = all spotlights use spotLightsCastShadows. 3 = only ~33% cast shadows.")]
    public int spotShadowEveryN = 0;

    [Header("Master toggles")]
    public bool setOn = true;
    public bool setUseSecondary = false;

    [Header("Master emission (applied to every ceiling)")]
    public Color masterPrimaryEmissionColor = Color.white;
    public float masterPrimaryEmissionMultiplier = 2f;
    public Color masterSecondaryEmissionColor = Color.red;
    public float masterSecondaryEmissionMultiplier = 2f;

    [Header("Master light colors/intensities")]
    public Color masterPrimarySpotColor = Color.white;
    public float masterPrimarySpotIntensity = 10f;
    public Color masterPrimaryPointColor = Color.white;
    public float masterPrimaryPointIntensity = 1f;

    public Color masterSecondarySpotColor = Color.red;
    public float masterSecondarySpotIntensity = 10f;
    public Color masterSecondaryPointColor = Color.red;
    public float masterSecondaryPointIntensity = 1f;

    [Header("Global intensity scales (fix 'too bright on Play')")]
    [Range(0f, 2f)] public float spotIntensityScale = 1f;
    [Range(0f, 2f)] public float pointIntensityScale = 1f;
    [Range(0f, 2f)] public float emissionMultiplierScale = 1f;

    CeilingLightController[] _ceilings;

    void Awake()
    {
        RefreshCache();
    }

    void Start()
    {
        if (applyOnStart) ApplyMasterToAll();
    }

    [ContextMenu("Refresh Ceilings Cache")]
    public void RefreshCache()
    {
        _ceilings = GetComponentsInChildren<CeilingLightController>(includeInactive);
    }

    [ContextMenu("Apply Master Settings To All Ceilings")]
    public void ApplyMasterToAll()
    {
        if (_ceilings == null || _ceilings.Length == 0) RefreshCache();

        foreach (var ceiling in _ceilings)
        {
            if (!ceiling) continue;

            // room overrides
            ceiling.treatAsFloor = treatAllAsFloor;
            ceiling.forceEmissionOff = forceAllEmissionOff;

            // basic state
            ceiling.SetOn(setOn);
            ceiling.SetUseSecondary(setUseSecondary);

            // emission (scaled)
            ceiling.SetPrimaryEmission(masterPrimaryEmissionColor, masterPrimaryEmissionMultiplier * emissionMultiplierScale);
            ceiling.SetSecondaryEmission(masterSecondaryEmissionColor, masterSecondaryEmissionMultiplier * emissionMultiplierScale);

            // light pairs
            if (ceiling.pairs == null) continue;

            foreach (var pair in ceiling.pairs)
            {
                if (!pair) continue;

                pair.SetPrimaryColors(masterPrimarySpotColor, masterPrimaryPointColor);
                pair.SetPrimaryIntensities(masterPrimarySpotIntensity * spotIntensityScale,
                                          masterPrimaryPointIntensity * pointIntensityScale);

                pair.SetSecondaryColors(masterSecondarySpotColor, masterSecondaryPointColor);
                pair.SetSecondaryIntensities(masterSecondarySpotIntensity * spotIntensityScale,
                                            masterSecondaryPointIntensity * pointIntensityScale);

                // shadows control (kills console spam)
                if (pair.spotLight)
                {
                    bool allow = spotLightsCastShadows;

                    if (spotShadowEveryN > 0)
                    {
                        int idx = pair.transform.GetSiblingIndex();
                        allow = (idx % spotShadowEveryN) == 0;
                    }

                    pair.spotLight.shadows = allow ? LightShadows.Soft : LightShadows.None;
                }

                if (pair.pointLight)
                {
                    pair.pointLight.shadows = pointLightsCastShadows ? LightShadows.Soft : LightShadows.None;
                }
            }
        }
    }
}