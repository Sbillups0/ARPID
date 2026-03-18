using UnityEngine;

public class WallSwapZone : MonoBehaviour
{
    public WallSwapGroup[] groups;

    void Reset()
    {
        // ensure trigger collider is set to trigger
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        foreach (var g in groups) g.SetInZone(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        foreach (var g in groups) g.SetInZone(false);
    }

    bool IsPlayer(Collider c)
    {
        var cam = Camera.main;
        if (cam == null) return false;

        return c.transform.root == cam.transform.root || c.transform.IsChildOf(cam.transform.root);
    }
}