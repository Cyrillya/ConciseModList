using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.UI;

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
            
            self.SearchFilterToggle.Left.Set(-30f, 1f);
            
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
        
        MonoModHooks.Add(typeof(UIMods).GetMethod("Populate", BindingFlags.Instance | BindingFlags.NonPublic), (Action<UIMods> orig, UIMods self) => {
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