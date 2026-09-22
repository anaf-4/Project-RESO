using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class SongConductor : MonoBehaviour
{
    [SerializeField] private string eventPath = "event:/Perfect_Run";

    private EventInstance instance;

    public float SongTimeSeconds { get; private set; }
    public bool IsPlaying { get; private set; }

    public void Play()
    {
        instance = RuntimeManager.CreateInstance(eventPath);
        instance.start();
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        instance.getTimelinePosition(out int positionMs);
        SongTimeSeconds = positionMs / 1000f;
    }

    private void OnDestroy()
    {
        if (!IsPlaying) return;
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
    }
}
