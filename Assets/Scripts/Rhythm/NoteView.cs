using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NoteView : MonoBehaviour
{
    public int Lane { get; private set; }
    public float HitTime { get; private set; }
    public bool IsResolved { get; private set; }

    private float spawnTime;
    private Vector3 spawnPosition;
    private Vector3 judgmentPosition;
    private SongConductor conductor;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(
        int lane, float hitTime, float spawnTime,
        Vector3 spawnPosition, Vector3 judgmentPosition,
        SongConductor conductor, Sprite sprite)
    {
        Lane = lane;
        HitTime = hitTime;
        this.spawnTime = spawnTime;
        this.spawnPosition = spawnPosition;
        this.judgmentPosition = judgmentPosition;
        this.conductor = conductor;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;

        transform.position = spawnPosition;
    }

    public void Resolve()
    {
        IsResolved = true;
        Destroy(gameObject);
    }

    private void Update()
    {
        if (IsResolved) return;
        transform.position = NoteMotion.ComputePosition(
            spawnTime, HitTime, conductor.SongTimeSeconds, spawnPosition, judgmentPosition);
    }
}
