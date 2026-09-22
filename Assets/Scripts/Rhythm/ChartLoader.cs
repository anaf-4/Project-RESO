using UnityEngine;

public static class ChartLoader
{
    public static ChartData LoadFromResources(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            throw new System.IO.FileNotFoundException(
                $"Chart not found at Resources/{resourcePath}.json");
        }
        return JsonUtility.FromJson<ChartData>(textAsset.text);
    }
}
