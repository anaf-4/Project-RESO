[System.Serializable]
public class NoteData
{
    public float time;
    public int lane;
}

[System.Serializable]
public class ChartData
{
    public NoteData[] notes;
}
