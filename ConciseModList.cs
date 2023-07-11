using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour.HookGen;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.Social.Steam;

namespace ConciseModList;

public class ConciseModList : Mod
{
    private static bool _unloading = false;

    public override void Load() {
        _unloading = false;

        HookEndpointManager.Add(typeof(UIMods).GetMethod("OnInitialize"), (Action<UIMods> orig, UIMods self) => {
            Interface.modsMenu.RemoveAllChildren();
            self._categoryButtons.Clear();

            orig(self);

            if (_unloading) return;

            // Repositioning filter buttons
            if (self._categoryButtons.Count >= 3) {
                for (int i = 0; i <= 2; i++) {
                    self._categoryButtons[i].Left.Pixels -= 8f;
                }
            }

            self.SearchFilterToggle.Left.Set(-30f, 1f);

            // Adding export mod list button
            var button =
                new UIImageButton(ModAsset.ExportListButton) {
                    Left = {Pixels = 107f},
                    Top = {Pixels = 10f},
                    _visibilityInactive = 0.8f
                };
            button.OnClick += (_, _) => ExportMods(true);
            button.OnRightClick += (_, _) => ExportMods(false);
            button.OnUpdate += element => {
                if (element.IsMouseHovering) {
                    Main.instance.MouseText(Language.GetTextValue("Mods.ConciseModList.ExportEnabledMods"));
                }
            };
            self.uIPanel.Append(button);

            self.uIElement.MaxWidth.Pixels = 620f;
            self.uIPanel.RemoveChild(self.uIPanel.Children.First(u => u is UIScrollbar));

            self.modList = new ImprovedUIList {
                Width = {Pixels = -25, Percent = 1f},
                Height = {Pixels = ModLoader.showMemoryEstimates ? -72 : -50, Percent = 1f},
                Top = {Pixels = ModLoader.showMemoryEstimates ? 72 : 50},
                ListPadding = 5f
            };
            var uIScrollbar = new UIScrollbar {
                Height = {Pixels = ModLoader.showMemoryEstimates ? -72 : -50, Percent = 1f},
                Top = {Pixels = ModLoader.showMemoryEstimates ? 72 : 50},
                HAlign = 1f
            }.WithView(100f, 1000f);
            self.uIPanel.Append(uIScrollbar);
            self.modList.SetScrollbar(uIScrollbar);
            self.uIPanel.Append(self.modList);

            self.Recalculate();
        });

        HookEndpointManager.Add(typeof(UIMods).GetMethod("Populate", BindingFlags.Instance | BindingFlags.NonPublic),
            (Action<UIMods> orig, UIMods self) => {
                // orig(self);

                Task.Run(() => {
                    var mods = ModOrganizer.FindMods(logDuplicates: true);
                    foreach (var mod in mods) {
                        var modItem = new ConciseUIModItem(mod);
                        modItem.Activate();
                        self.items.Add(modItem);
                    }

                    self.needToRemoveLoading = true;
                    self.updateNeeded = true;
                    loading = false;
                });
            });

        Interface.modsMenu.OnInitialize();
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
        System.IO.File.WriteAllText(fullPath, modList);

        if (!System.IO.File.Exists(fullPath)) return;
        Process.Start(new ProcessStartInfo(fullPath) {
            UseShellExecute = true
        });
    }

    public override void Unload() {
        _unloading = true;
        Interface.modsMenu._categoryButtons.Clear();
        Interface.modsMenu.RemoveAllChildren();
        Interface.modsMenu.OnInitialize();
    }
}