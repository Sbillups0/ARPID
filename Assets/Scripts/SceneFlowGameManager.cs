using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowGameManager : MonoBehaviour
{
    public static SceneFlowGameManager Instance { get; private set; }

    [Header("Lighting sequences")]
    public float preTransitionFlickerSeconds = 4f;
    public float arrivalFlickerSeconds = 4f;

    [Header("Optional fade")]
    public bool useDeathFade = true;
    public float fadeFromBlackOnArrival = 0.35f;

    string _currentRespawnId = "";
    string _pendingSpawnId = "";
    bool _playArrivalSequenceOnLoad;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void RegisterRespawnPoint(string respawnId)
    {
        _currentRespawnId = respawnId;
    }

    public void TransitionToScene(string targetSceneName, string targetSpawnId = "")
    {
        StartCoroutine(TransitionRoutine(targetSceneName, targetSpawnId));
    }

    public void RespawnCurrentScene()
    {
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator TransitionRoutine(string targetSceneName, string targetSpawnId)
    {
        yield return FlickerSceneLights(preTransitionFlickerSeconds);

        SetSceneLightsOff();

        _pendingSpawnId = targetSpawnId;
        _playArrivalSequenceOnLoad = true;

        if (useDeathFade && DeathFade.Instance != null)
            DeathFade.Instance.FadeAndLoadScene(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }

    IEnumerator RespawnRoutine()
    {
        _pendingSpawnId = _currentRespawnId;
        _playArrivalSequenceOnLoad = true;

        if (useDeathFade && DeathFade.Instance != null)
            DeathFade.Instance.FadeAndReloadCurrentScene();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        yield break;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneLoaded());
    }

    IEnumerator HandleSceneLoaded()
    {
        yield return null;

        MovePlayerToSpawnPoint();

        if (_playArrivalSequenceOnLoad)
        {
            yield return ArrivalSequence();
            _playArrivalSequenceOnLoad = false;
        }

        if (useDeathFade && DeathFade.Instance != null)
            yield return DeathFade.Instance.FadeFromBlack(fadeFromBlackOnArrival);
    }

    void MovePlayerToSpawnPoint()
    {
        SceneFlowTrigger spawn = FindSpawnPointInScene(_pendingSpawnId);

        if (spawn == null)
        {
            _pendingSpawnId = "";
            return;
        }

        Transform playerRoot = FindPlayerRoot();
        if (playerRoot == null)
        {
            _pendingSpawnId = "";
            return;
        }

        Transform target = spawn.GetSpawnTransform();
        if (target == null)
        {
            _pendingSpawnId = "";
            return;
        }

        CharacterController cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerRoot.position = target.position;

        Vector3 euler = playerRoot.eulerAngles;
        euler.y = target.eulerAngles.y;
        playerRoot.eulerAngles = euler;

        if (cc != null) cc.enabled = true;

        if (!string.IsNullOrEmpty(spawn.respawnId))
            _currentRespawnId = spawn.respawnId;

        _pendingSpawnId = "";
    }

    SceneFlowTrigger FindSpawnPointInScene(string requestedId)
    {
        SceneFlowTrigger[] all = FindObjectsByType<SceneFlowTrigger>(FindObjectsSortMode.None);

        SceneFlowTrigger defaultSpawn = null;

        foreach (var t in all)
        {
            if (t == null) continue;
            if (t.mode != SceneFlowTrigger.TriggerMode.RespawnPoint) continue;

            if (!string.IsNullOrEmpty(requestedId) && t.respawnId == requestedId)
                return t;

            if (t.isDefaultRespawn)
                defaultSpawn = t;
        }

        if (defaultSpawn != null)
            return defaultSpawn;

        foreach (var t in all)
        {
            if (t == null) continue;
            if (t.mode == SceneFlowTrigger.TriggerMode.RespawnPoint)
                return t;
        }

        return null;
    }

    Transform FindPlayerRoot()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged.transform.root;

        if (Camera.main != null)
            return Camera.main.transform.root;

        return null;
    }

    IEnumerator ArrivalSequence()
    {
        CeilingLightController[] ceilings = GetActiveSceneCeilings();

        foreach (var c in ceilings)
        {
            if (!c) continue;
            c.SetOn(true);
            c.SetUseSecondary(false);
            c.SetFlicker(true);
        }

        yield return new WaitForSeconds(arrivalFlickerSeconds);

        foreach (var c in ceilings)
        {
            if (!c) continue;
            c.SetFlicker(false);
            c.SetOn(true);
            c.SetUseSecondary(false);
        }
    }

    IEnumerator FlickerSceneLights(float seconds)
    {
        CeilingLightController[] ceilings = GetActiveSceneCeilings();

        foreach (var c in ceilings)
        {
            if (!c) continue;
            c.SetOn(true);
            c.SetUseSecondary(false);
            c.SetFlicker(true);
        }

        yield return new WaitForSeconds(seconds);

        foreach (var c in ceilings)
        {
            if (!c) continue;
            c.SetFlicker(false);
        }
    }

    void SetSceneLightsOff()
    {
        CeilingLightController[] ceilings = GetActiveSceneCeilings();

        foreach (var c in ceilings)
        {
            if (!c) continue;
            c.SetOn(false);
        }
    }

    CeilingLightController[] GetActiveSceneCeilings()
    {
        var all = FindObjectsByType<CeilingLightController>(FindObjectsSortMode.None);
        List<CeilingLightController> result = new List<CeilingLightController>();
        Scene active = SceneManager.GetActiveScene();

        foreach (var c in all)
        {
            if (c != null && c.gameObject.scene == active)
                result.Add(c);
        }

        return result.ToArray();
    }
}