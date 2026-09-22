using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SongConductor : MonoBehaviour
{
    [SerializeField] private string eventPath = "event:/Perfect_Run";
    [SerializeField] private float offsetMs = 0f;

    private EventInstance instance;

    public float SongTimeSeconds { get; private set; }
    public bool IsPlaying { get; private set; }

    public void Play()
    {
        if (IsPlaying) return;
        instance = RuntimeManager.CreateInstance(eventPath);
        instance.start();
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        instance.getTimelinePosition(out int positionMs);
        SongTimeSeconds = (positionMs - offsetMs) / 1000f;
    }

    private void OnDestroy()
    {
        if (!IsPlaying) return;
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
    }
}
