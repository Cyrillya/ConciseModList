using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.Social.Steam;
using Terraria.UI;

namespace ConciseModList;

public class ExportListButton () : UIImageButton(ModAsset.ExportListButton)
{
    public override void LeftClick(UIMouseEvent evt) {
        base.LeftClick(evt);
        ExportMods(true);
    }

    public override void RightClick(UIMouseEvent evt) {
        base.RightClick(evt);
        ExportMods(false);
    }

    private void ExportMods(bool withWorkshopLink) {
        const string fileName = "enabledMods.txt";
        string fullPath = Path.Combine(ModLoader.ModPath, fileName);

        string modList = "";
        var mods = ModOrganizer.FindMods(logDuplicates: true);
        foreach (var mod in mods) {
            if (!mod.Enabled) continue;

            modList += $"{mod.DisplayName} v{mod.modFile.Version} ({mod.Name})";
            if (withWorkshopLink && WorkshopHelper.GetPublishIdLocal(mod.modFile, out ulong publishId))
                modList += $"\n  - Steam: https://steamcommunity.com/sharedfiles/filedetails/?id={publishId}";

            modList += "\n";
        }

        Directory.CreateDirectory(ModLoader.ModPath);
        File.WriteAllText(fullPath, modList);

        if (!File.Exists(fullPath)) return;
        Process.Start(new ProcessStartInfo(fullPath) {
            UseShellExecute = true
        });
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        base.DrawSelf(spriteBatch);

        if (IsMouseHovering) {
            UICommon.TooltipMouseText(Language.GetTextValue("Mods.ConciseModList.ExportEnabledMods"));
        }
    }
}