using UnityEngine;

public class SceneFlowTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        RespawnPoint,
        TransitionToScene
    }

    [Header("Mode")]
    public TriggerMode mode = TriggerMode.RespawnPoint;

    [Header("Respawn")]
    public string respawnId = "DefaultRespawn";
    public bool isDefaultRespawn = true;
    public Transform spawnTransformOverride;

    [Header("Transition")]
    public string targetSceneName;
    public string targetSpawnIdInNextScene = "";

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool oneShot = false;

    bool _triggered;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered && oneShot) return;
        if (!IsPlayer(other)) return;

        if (mode == TriggerMode.RespawnPoint)
        {
            if (SceneFlowGameManager.Instance != null)
                SceneFlowGameManager.Instance.RegisterRespawnPoint(respawnId);
        }
        else if (mode == TriggerMode.TransitionToScene)
        {
            if (SceneFlowGameManager.Instance != null && !string.IsNullOrEmpty(targetSceneName))
                SceneFlowGameManager.Instance.TransitionToScene(targetSceneName, targetSpawnIdInNextScene);
        }

        if (oneShot)
            _triggered = true;
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        if (other.transform.root.CompareTag(playerTag)) return true;

        if (Camera.main != null)
        {
            Transform camRoot = Camera.main.transform.root;
            if (other.transform.root == camRoot || other.transform.IsChildOf(camRoot))
                return true;
        }

        return false;
    }

    public Transform GetSpawnTransform()
    {
        return spawnTransformOverride != null ? spawnTransformOverride : transform;
    }
}