using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Looping3DAudioSegment : MonoBehaviour
{
    [Header("Clip")]
    public AudioClip clip;

    [Header("Playback range in seconds")]
    public float startTime = 0f;
    public float endTime = 1f;

    [Header("Playback")]
    public bool playOnStart = true;
    public bool loopSegment = true;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 0.5f;

    [Header("3D Audio")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    public float minDistance = 2f;
    public float maxDistance = 20f;
    public float dopplerLevel = 0f;

    AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false; // handled by script

        ApplyAudioSettings();
    }

    void OnValidate()
    {
        if (_source == null)
            _source = GetComponent<AudioSource>();

        if (_source != null)
            ApplyAudioSettings();
    }

    void Start()
    {
        if (clip != null)
            _source.clip = clip;

        ClampTimes();

        if (playOnStart)
            PlaySegment();
    }

    void Update()
    {
        if (_source.clip == null || !_source.isPlaying)
            return;

        if (_source.time >= endTime)
        {
            if (loopSegment)
            {
                _source.time = startTime;
                _source.Play();
            }
            else
            {
                _source.Stop();
            }
        }
    }

    void ApplyAudioSettings()
    {
        _source.volume = volume;
        _source.spatialBlend = spatialBlend;
        _source.rolloffMode = rolloffMode;
        _source.minDistance = minDistance;
        _source.maxDistance = maxDistance;
        _source.dopplerLevel = dopplerLevel;
    }

    void ClampTimes()
    {
        if (_source.clip == null)
            return;

        startTime = Mathf.Clamp(startTime, 0f, _source.clip.length);
        endTime = Mathf.Clamp(endTime, 0f, _source.clip.length);

        if (endTime <= startTime)
            endTime = Mathf.Min(startTime + 0.05f, _source.clip.length);
    }

    public void PlaySegment()
    {
        if (_source.clip == null)
            return;

        ClampTimes();
        ApplyAudioSettings();

        _source.time = startTime;
        _source.Play();
    }

    public void StopSegment()
    {
        _source.Stop();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (_source != null) _source.volume = volume;
    }

    public void SetSegment(float newStart, float newEnd)
    {
        startTime = newStart;
        endTime = newEnd;
        ClampTimes();
    }
}