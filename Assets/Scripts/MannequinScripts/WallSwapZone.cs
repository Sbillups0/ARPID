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
        // XR Origin typically has colliders on the CharacterController/capsule under the XROrigin.
        // Easiest robust check: compare to root that has Camera.main under it.
        var cam = Camera.main;
        if (cam == null) return false;

        // If the collider belongs to the same XR rig hierarchy as the camera
        return c.transform.root == cam.transform.root || c.transform.IsChildOf(cam.transform.root);
    }
}