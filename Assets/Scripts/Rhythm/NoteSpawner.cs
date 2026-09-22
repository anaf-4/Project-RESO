using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private SongConductor conductor;
    [SerializeField] private JudgmentSystem judgmentSystem;
    [SerializeField] private NoteView notePrefab;
    [SerializeField] private Transform[] laneSpawnPoints = new Transform[4];
    [SerializeField] private Transform[] laneJudgmentPoints = new Transform[4];
    [SerializeField] private Sprite[] laneNoteSprites = new Sprite[4];
    [SerializeField] private float leadTimeSeconds = 2f;
    [SerializeField] private string chartResourcePath = "Charts/perfect_run_test";

    private ChartData chart;
    private int nextNoteIndex;

    private void Awake()
    {
        chart = ChartLoader.LoadFromResources(chartResourcePath);
        conductor.Play();
    }

    private void Update()
    {
        if (chart?.notes == null) return;

        while (nextNoteIndex < chart.notes.Length &&
               chart.notes[nextNoteIndex].time - leadTimeSeconds <= conductor.SongTimeSeconds)
        {
            SpawnNote(chart.notes[nextNoteIndex]);
            nextNoteIndex++;
        }
    }

    private void SpawnNote(NoteData data)
    {
        float spawnTime = data.time - leadTimeSeconds;
        Transform spawnPoint = laneSpawnPoints[data.lane];
        Transform judgmentPoint = laneJudgmentPoints[data.lane];

        NoteView note = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
        note.Initialize(
            data.lane, data.time, spawnTime,
            spawnPoint.position, judgmentPoint.position,
            conductor, laneNoteSprites[data.lane]);

        judgmentSystem.Register(note);
    }
}
