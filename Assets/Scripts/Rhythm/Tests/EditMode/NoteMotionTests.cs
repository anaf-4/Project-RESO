using NUnit.Framework;
using UnityEngine;

public class NoteMotionTests
{
    [Test]
    public void ComputePosition_AtSpawnTime_ReturnsSpawnPosition()
    {
        Vector3 result = NoteMotion.ComputePosition(
            spawnTime: 0f, hitTime: 2f, now: 0f,
            spawnPosition: new Vector3(0, 5, 0), judgmentPosition: new Vector3(0, -3, 0));

        Assert.AreEqual(new Vector3(0, 5, 0), result);
    }

    [Test]
    public void ComputePosition_AtHitTime_ReturnsJudgmentPosition()
    {
        Vector3 result = NoteMotion.ComputePosition(
            spawnTime: 0f, hitTime: 2f, now: 2f,
            spawnPosition: new Vector3(0, 5, 0), judgmentPosition: new Vector3(0, -3, 0));

        Assert.AreEqual(new Vector3(0, -3, 0), result);
    }

    [Test]
    public void ComputePosition_Halfway_ReturnsMidpoint()
    {
        Vector3 result = NoteMotion.ComputePosition(
            spawnTime: 0f, hitTime: 2f, now: 1f,
            spawnPosition: new Vector3(0, 5, 0), judgmentPosition: new Vector3(0, -3, 0));

        Assert.AreEqual(new Vector3(0, 1, 0), result);
    }
}
