using UnityEngine;

public class TriggerSwapParents : MonoBehaviour
{
    [Header("Walls to remove/disable")]
    [SerializeField] private GameObject[] wallsToDisable;
    [Header("Walls to enable (optional)")]
    [SerializeField] private GameObject[] wallsToEnable;

    [Header("Mannequin parents")]
    [SerializeField] private GameObject oldMannequinParent;
    [SerializeField] private GameObject newMannequinParent;

    [Header("Trigger settings")]
    [SerializeField] private string triggeringTag = "Player";
    [SerializeField] private bool oneShot = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && hasTriggered) return;
        if (!other.CompareTag(triggeringTag)) return;

        hasTriggered = true;

        // Disable all walls
        if (wallsToDisable != null)
        {
            for (int i = 0; i < wallsToDisable.Length; i++)
            {
                if (wallsToDisable[i] != null)
                    wallsToDisable[i].SetActive(false); // or Destroy(wallsToDisable[i]);
            }
        }

        // Enable all walls
        if (wallsToEnable != null)
        {
            for (int i = 0; i < wallsToEnable.Length; i++)
            {
                if (wallsToEnable[i] != null)
                    wallsToEnable[i].SetActive(true);
            }
        }
        // Swap mannequin parents
        if (oldMannequinParent) oldMannequinParent.SetActive(false);
        if (newMannequinParent) newMannequinParent.SetActive(true);

        // Optional: disable the trigger collider so it can't fire again
        if (oneShot)
        {
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }
}