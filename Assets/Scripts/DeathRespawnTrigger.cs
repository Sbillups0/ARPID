using UnityEngine;

public class DeathRespawnTrigger : MonoBehaviour
{
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

        if (SceneFlowGameManager.Instance != null)
            SceneFlowGameManager.Instance.RespawnCurrentScene();

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
}