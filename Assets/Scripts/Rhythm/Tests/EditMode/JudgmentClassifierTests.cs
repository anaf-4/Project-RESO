using NUnit.Framework;

public class JudgmentClassifierTests
{
    [TestCase(0f, Grade.Perfect)]
    [TestCase(18f, Grade.Perfect)]
    [TestCase(-18f, Grade.Perfect)]
    [TestCase(18.1f, Grade.Great)]
    [TestCase(-40f, Grade.Great)]
    [TestCase(40.1f, Grade.Good)]
    [TestCase(75f, Grade.Good)]
    [TestCase(75.1f, Grade.Bad)]
    [TestCase(120f, Grade.Bad)]
    [TestCase(120.1f, Grade.Miss)]
    [TestCase(500f, Grade.Miss)]
    [TestCase(-500f, Grade.Miss)]
    public void Classify_ReturnsExpectedGrade(float deltaMs, Grade expected)
    {
        Assert.AreEqual(expected, JudgmentClassifier.Classify(deltaMs));
    }
}
