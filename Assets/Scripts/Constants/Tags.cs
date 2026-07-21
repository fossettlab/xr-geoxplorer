/// <summary>
/// Canonical GameObject tag strings used across the scene. Reference these
/// constants instead of bare string literals so renames are compiler-checked
/// and "find references" works. Values must match ProjectSettings/TagManager.asset.
/// </summary>
public static class Tags
{
    public const string ActiveModel = "activeModel";
    public const string AssetBundle = "AssetBundle";
    public const string AssetBundleLoader = "AssetBundleLoader";
    public const string Flag = "flag";
    public const string FlagPrime = "flagPrime";
    public const string GoToTooltip = "GoToTooltip";
    public const string InfoMarker = "InfoMarker";
    public const string MapTile = "MapTile";
    public const string OutcropTooltip = "OutcropTooltip";
    public const string Tappable = "tappable";
    public const string TilePlane = "TilePlane";
    public const string TooltipInteraction = "TooltipInteraction";
}
