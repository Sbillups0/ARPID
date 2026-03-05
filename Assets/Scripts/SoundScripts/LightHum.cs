using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LightHum : MonoBehaviour
{
    public AudioClip humClip;
    [Range(0f, 1f)] public float volume = 0.35f;
    public bool playOnStart = true;

    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.clip = humClip;
        src.loop = true;
        src.playOnAwake = false;

        // 3D settings
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;

        src.volume = volume;
    }

    void Start()
    {
        if (playOnStart && humClip != null)
            src.Play();
    }
}