using NUnit.Framework;
using UnityEngine;

public class JudgmentSystemTests
{
    private static JudgmentSystem CreateSystem()
    {
        var go = new GameObject("JudgmentSystem");
        return go.AddComponent<JudgmentSystem>();
    }

    private static NoteView CreateNote(int lane, float hitTime)
    {
        var go = new GameObject("NoteView");
        var note = go.AddComponent<NoteView>();
        note.Initialize(lane, hitTime, hitTime - 2f, Vector3.zero, Vector3.zero, null, null);
        return note;
    }

    [Test]
    public void TryHit_WithinPerfectWindow_FiresPerfectAndConsumesNote()
    {
        JudgmentSystem system = CreateSystem();
        NoteView note = CreateNote(lane: 0, hitTime: 10f);
        system.Register(note);

        Grade? firedGrade = null;
        system.OnJudged.AddListener((lane, grade) => firedGrade = grade);

        system.TryHit(lane: 0, inputTimeSeconds: 10.01f);

        Assert.AreEqual(Grade.Perfect, firedGrade);
    }

    [Test]
    public void TryHit_OutsideBadWindow_DoesNotFire()
    {
        JudgmentSystem system = CreateSystem();
        NoteView note = CreateNote(lane: 1, hitTime: 10f);
        system.Register(note);

        bool fired = false;
        system.OnJudged.AddListener((_, __) => fired = true);

        system.TryHit(lane: 1, inputTimeSeconds: 10.5f);

        Assert.IsFalse(fired);
    }

    [Test]
    public void TryHit_OnEmptyLaneQueue_DoesNotThrow()
    {
        JudgmentSystem system = CreateSystem();
        Assert.DoesNotThrow(() => system.TryHit(lane: 2, inputTimeSeconds: 5f));
    }
}
