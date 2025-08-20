using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ConciseModList;

public class ConciseModConfig : ModConfig
{
    public static ConciseModConfig Instance => ModContent.GetInstance<ConciseModConfig>();
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)] public bool EnabledModsFirst;
    [DefaultValue(true)] public bool ConfigButton;
    [DefaultValue(true)] public bool SteamIcon;
    [DefaultValue(true)] public bool ModpackIcon;
    [DefaultValue(true)] public bool UpdateDot;
    [DefaultValue(true)] public bool ObsidianSkull;
    [DefaultValue(true)] public bool ModReferenceIcon;
    [DefaultValue(true)] public bool TranslationModIcon;
}