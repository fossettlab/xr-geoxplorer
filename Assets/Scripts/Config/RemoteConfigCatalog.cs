using UnityEngine;

/// <summary>
/// Resources-loadable catalog that points at the Dev/Staging/Prod RemoteConfig assets.
/// </summary>
[CreateAssetMenu(fileName = "RemoteConfigCatalog", menuName = "GeoXplorer/Remote Config Catalog")]
public class RemoteConfigCatalog : ScriptableObject
{
    [SerializeField] private RemoteConfig dev;
    [SerializeField] private RemoteConfig staging;
    [SerializeField] private RemoteConfig prod;

    public RemoteConfig Dev => dev;
    public RemoteConfig Staging => staging;
    public RemoteConfig Prod => prod;
}
