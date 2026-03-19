using UnityEngine;

public class FootStepPlayer : MonoBehaviour
{
    [Header("Refs")]
    public Transform rigRoot;          // the object that moves 
    public AudioSource footstepSource; // can be on rigRoot
    public AudioClip[] footstepClips;

    [Header("Tuning")]
    public float stepDistance = 1.6f;  // meters per step at normal speed
    public float minSpeed = 0.15f;     // ignore tiny drift
    public bool use2D = true;

    private Vector3 lastPos;
    private float accumulatedDistance;

    void Awake()
    {
        if (rigRoot == null) rigRoot = transform;
        lastPos = rigRoot.position;

        if (footstepSource != null)
            footstepSource.spatialBlend = use2D ? 0f : 1f;
    }

    void Update()
    {
        if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        Vector3 delta = rigRoot.position - lastPos;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        // Optionally ignore vertical motion
        delta.y = 0f;

        if (speed < minSpeed)
        {
            accumulatedDistance = 0f;
            lastPos = rigRoot.position;
            return;
        }

        accumulatedDistance += delta.magnitude;

        if (accumulatedDistance >= stepDistance)
        {
            PlayFootstep();
            accumulatedDistance = 0f;
        }

        lastPos = rigRoot.position;
    }

    void PlayFootstep()
    {
        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        footstepSource.pitch = Random.Range(0.95f, 1.05f);
        footstepSource.PlayOneShot(clip, 1f);
    }
}