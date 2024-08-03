using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
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

        MonoModHooks.Add(typeof(UIMods).GetMethod("OnInitialize"), (Action<UIMods> orig, UIMods self) => {
            Interface.modsMenu.RemoveAllChildren();
            self._categoryButtons.Clear();

            orig(self);

            if (_unloading) return;

            // Repositioning filter buttons
            if (self._categoryButtons.Count >= 4) {
                for (int i = 0; i <= 3; i++) {
                    self._categoryButtons[i].Left.Pixels -= 8f;
                }
            }

            self.SearchFilterToggle.Left.Set(-30f, 1f);

            // Adding export mod list button
            var button =
                new ExportListButton {
                    Left = {Pixels = 136f},
                    Top = {Pixels = 10f},
                    _visibilityInactive = 0.8f
                };
            self.uIPanel.Append(button);

            self.uIElement.MaxWidth.Pixels = 620f;
            self.uIPanel.RemoveChild(self.uIPanel.Children.First(u => u is UIScrollbar));

            self.modList = new ImprovedUIList {
                Width = {Pixels = -25, Percent = 1f},
                Height = { Pixels = -50, Percent = 1f },
                Top = { Pixels = 50 },
                ListPadding = 5f
            };
            self.uIPanel.Append(self.modList);

            self.uIScrollbar = new UIScrollbar {
                Height = { Pixels = -50, Percent = 1f },
                Top = { Pixels = 50 },
                HAlign = 1f
            }.WithView(100f, 1000f);
            self.uIPanel.Append(self.uIScrollbar);
            self.modList.SetScrollbar(self.uIScrollbar);

            self.Recalculate();
        });

        MonoModHooks.Add(typeof(UIMods).GetMethod("Populate", BindingFlags.Instance | BindingFlags.NonPublic),
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


    public override void Unload() {
        _unloading = true;
        Interface.modsMenu._categoryButtons.Clear();
        Interface.modsMenu.RemoveAllChildren();
        Interface.modsMenu.OnInitialize();
    }
}