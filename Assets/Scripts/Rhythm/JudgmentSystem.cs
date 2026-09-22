using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class JudgmentEvent : UnityEvent<int, Grade> { }

public class JudgmentSystem : MonoBehaviour
{
    [SerializeField] private SongConductor conductor;
    public JudgmentEvent OnJudged = new JudgmentEvent();

    private readonly List<Queue<NoteView>> laneQueues = new List<Queue<NoteView>>
    {
        new Queue<NoteView>(), new Queue<NoteView>(), new Queue<NoteView>(), new Queue<NoteView>()
    };

    public void Register(NoteView note)
    {
        laneQueues[note.Lane].Enqueue(note);
    }

    public void TryHit(int lane, float inputTimeSeconds)
    {
        Queue<NoteView> queue = laneQueues[lane];
        if (queue.Count == 0) return;

        NoteView note = queue.Peek();
        float deltaMs = (inputTimeSeconds - note.HitTime) * 1000f;
        Grade grade = JudgmentClassifier.Classify(deltaMs);
        if (grade == Grade.Miss) return;

        queue.Dequeue();
        note.Resolve();
        OnJudged.Invoke(lane, grade);
    }

    private void Update()
    {
        if (conductor == null) return;
        float now = conductor.SongTimeSeconds;
        float missWindowSeconds = JudgmentClassifier.BadWindowMs / 1000f;

        foreach (Queue<NoteView> queue in laneQueues)
        {
            while (queue.Count > 0 && now - queue.Peek().HitTime > missWindowSeconds)
            {
                NoteView missed = queue.Dequeue();
                int lane = missed.Lane;
                missed.Resolve();
                OnJudged.Invoke(lane, Grade.Miss);
            }
        }
    }
}
