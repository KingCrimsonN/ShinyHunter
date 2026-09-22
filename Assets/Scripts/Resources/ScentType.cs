/// <summary>
/// The five scent axes stews/ingredients are measured on. Order MUST match
/// StewInstance.scents' index order and StewDisplayUtil.ScentLabels (Sweet,
/// Fresh, Putrid, Metallic, Marine).
///
/// ResourceData keeps its five scent fields as plain floats rather than
/// switching to this enum - an INGREDIENT contributes to potentially all five
/// axes at once (that's still a full profile). This enum is for the CREATURE
/// side, where each species now cares about exactly one favorite and one
/// hated axis - see CreatureData.favoriteScent / hatedScent.
/// </summary>
public enum ScentType
{
    Sweet,
    Fresh,
    Putrid,
    Metallic,
    Marine
}
