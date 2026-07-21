using UnityEngine;

/// <summary>
/// Centralized tag/name scene queries. Dynamic multi-object sets (tooltips, flags, tiles)
/// still require a scene scan; call sites should use these helpers instead of raw Find*.
/// </summary>
public static class SceneQueries
{
    public static GameObject[] WithTag(string tag)
    {
        return GameObject.FindGameObjectsWithTag(tag);
    }

    public static GameObject OneWithTag(string tag)
    {
        return GameObject.FindGameObjectWithTag(tag);
    }

    public static bool AnyWithTag(string tag)
    {
        return GameObject.FindGameObjectWithTag(tag) != null;
    }

    public static GameObject ByName(string objectName)
    {
        return GameObject.Find(objectName);
    }
}
