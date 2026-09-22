using NUnit.Framework;

public class ChartLoaderTests
{
    [Test]
    public void LoadFromResources_ParsesTestChart()
    {
        ChartData chart = ChartLoader.LoadFromResources("Charts/perfect_run_test");

        Assert.IsNotNull(chart);
        Assert.IsNotNull(chart.notes);
        Assert.GreaterOrEqual(chart.notes.Length, 15);
        Assert.AreEqual(2.5f, chart.notes[0].time, 0.0001f);
        Assert.AreEqual(0, chart.notes[0].lane);
    }
}
