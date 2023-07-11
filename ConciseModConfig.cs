using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace ConciseModList;

[Label("$Mods.ConciseModList.Configs.ConciseModConfig.DisplayName")]
public class ConciseModConfig : ModConfig
{
    public static ConciseModConfig Instance => ModContent.GetInstance<ConciseModConfig>();
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Label("$Mods.ConciseModList.Configs.ConciseModConfig.ConfigButton.Label")]
    [Tooltip("$Mods.ConciseModList.Configs.ConciseModConfig.ConfigButton.Tooltip")]
    [DefaultValue(true)]
    public bool ConfigButton;

    [Label("$Mods.ConciseModList.Configs.ConciseModConfig.SteamIcon.Label")]
    [Tooltip("$Mods.ConciseModList.Configs.ConciseModConfig.SteamIcon.Tooltip")]
    [DefaultValue(true)]
    public bool SteamIcon;

    [Label("$Mods.ConciseModList.Configs.ConciseModConfig.UpdateDot.Label")]
    [Tooltip("$Mods.ConciseModList.Configs.ConciseModConfig.UpdateDot.Tooltip")]
    [DefaultValue(true)]
    public bool UpdateDot;

    [Label("$Mods.ConciseModList.Configs.ConciseModConfig.PurpleBackground.Label")]
    [Tooltip("$Mods.ConciseModList.Configs.ConciseModConfig.PurpleBackground.Tooltip")]
    [DefaultValue(true)]
    public bool PurpleBackground;
}