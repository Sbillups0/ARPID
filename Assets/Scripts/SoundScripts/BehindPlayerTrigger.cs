using UnityEngine;

public class BehindPlayerTrigger : MonoBehaviour
{
    public AudioClip soundClip;
    public Transform playerCamera;  // assign your XR camera transform
    public float behindDistance = 2.0f;
    public float volume = 1.0f;
    public bool oneTime = true;

    private bool fired;

    private void OnTriggerEnter(Collider other)
    {
        if (fired && oneTime) return;

        // Option A: simplest filter: check tag on rig root
        if (!other.CompareTag("Player")) return;

        FireKnock();
    }

    public void FireKnock()
    {
        if (soundClip == null || playerCamera == null) return;

        Vector3 pos = playerCamera.position - playerCamera.forward * behindDistance;

        // Create a temporary audio object
        GameObject go = new GameObject("TempKnock");
        go.transform.position = pos;

        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;

        // Tune distances so it feels close but behind you
        src.minDistance = 0.5f;
        src.maxDistance = 12f;

        src.PlayOneShot(soundClip, volume);

        Destroy(go, soundClip.length + 0.2f);

        fired = true;
    }
}