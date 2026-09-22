using UnityEngine;

public static class NoteMotion
{
    public static Vector3 ComputePosition(
        float spawnTime, float hitTime, float now,
        Vector3 spawnPosition, Vector3 judgmentPosition)
    {
        float t = hitTime > spawnTime ? (now - spawnTime) / (hitTime - spawnTime) : 1f;
        return Vector3.LerpUnclamped(spawnPosition, judgmentPosition, t);
    }
}
