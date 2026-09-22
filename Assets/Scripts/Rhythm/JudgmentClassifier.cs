public static class JudgmentClassifier
{
    public const float PerfectWindowMs = 18f;
    public const float GreatWindowMs = 40f;
    public const float GoodWindowMs = 75f;
    public const float BadWindowMs = 120f;

    public static Grade Classify(float deltaMs)
    {
        float abs = System.Math.Abs(deltaMs);
        if (abs <= PerfectWindowMs) return Grade.Perfect;
        if (abs <= GreatWindowMs) return Grade.Great;
        if (abs <= GoodWindowMs) return Grade.Good;
        if (abs <= BadWindowMs) return Grade.Bad;
        return Grade.Miss;
    }
}
