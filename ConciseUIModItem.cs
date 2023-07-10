using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace ConciseModList;

public class ConciseUIModItem : UIModItem
{
    public ConciseUIModItem(LocalMod mod) : base(mod) {
        Width.Set(90f, 0f);
    }

    public override void OnInitialize() {
        _modIcon = new UIImage(Main.Assets
            .Request<Texture2D>("Images/UI/DefaultResourcePackIcon", AssetRequestMode.ImmediateLoad).Value) {
            Left = {Pixels = 9},
            Top = {Pixels = 9},
            ImageScale = 1.3f
        };
        _modIconAdjust += 85;
        Append(_modIcon);

        if (_mod.modFile.HasFile("icon.png")) {
            Task.Run(() => {
                try {
                    using (_mod.modFile.Open())
                    using (var s = _mod.modFile.GetStream("icon.png")) {
                        var iconTexture = Main.Assets.CreateUntracked<Texture2D>(s, ".png").Value;

                        if (iconTexture.Width == 80 && iconTexture.Height == 80) {
                            _modIcon.ImageScale = 1f;
                            _modIcon.Left.Pixels = -1;
                            _modIcon.Top.Pixels = -1;
                            _modIcon.SetImage(iconTexture);
                        }
                    }
                }
                catch (Exception e) {
                    Logging.tML.Error("Unknown error", e);
                }
            });
        }

        _uiModStateText = new UIModStateText(_mod.Enabled) {
            Top = {Pixels = 114514}
        };
        _uiModStateText.OnLeftClick += ToggleEnabled;

        _moreInfoButton = new UIImage(UICommon.ButtonModInfoTexture) {
            Top = {Pixels = 114514}
        };
        _moreInfoButton.OnLeftClick += ShowMoreInfo;

        if (ModLoader.TryGetMod(ModName, out var loadedMod) && ConfigManager.Configs.ContainsKey(loadedMod)) {
            OnRightClick += OpenConfig;
            if (ConfigManager.ModNeedsReload(loadedMod)) {
                _configChangesRequireReload = true;
            }
        }

        if (ModOrganizer.CheckStableBuildOnPreview(_mod)) {
            _keyImage = new UIHoverImage(Main.Assets.Request<Texture2D>(TextureAssets.Item[ItemID.LavaSkull].Name),
                "");

            Append(_keyImage);
        }

        if (_mod.modFile.path.StartsWith(ModLoader.ModPath)) {
            BackgroundColor = Color.MediumPurple * 0.7f;
            modFromLocalModFolder = true;
        }
        else {
            var steamIcon = new UIImage(TextureAssets.Extra[243]) {
                Left = {Pixels = -22, Percent = 1f}
            };
            Append(steamIcon);
        }

        if (loadedMod != null) {
            _loaded = true;
        }

        var oldModVersionData = ModOrganizer.modsThatUpdatedSinceLastLaunch.FirstOrDefault(x => x.ModName == ModName);
        if (oldModVersionData != default) {
            previousVersionHint = oldModVersionData.previousVersion;
            var toggleImage = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
            updatedModDot = new UIImageFramed(toggleImage, toggleImage.Frame(2, 1, 1, 0)) {
                Left = {Pixels = 2f, Percent = 0f},
                Top = {Pixels = -18f, Percent = 1f},
                Color = previousVersionHint == null ? Color.Green : new Color(6, 95, 212)
            };
            Append(updatedModDot);
        }

        _modReferences = _mod.properties.modReferences.Select(x => x.mod).ToArray();

        if (_modReferences.Length > 0 && !_mod.Enabled) {
            OnMiddleClick += EnableDependencies;
        }

        OnLeftClick += (e, _) => { _uiModStateText.LeftClick(e); };

        if (!_loaded) {
            OnRightClick += QuickModDelete;
        }
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        float opacity = _mod.Enabled ? 1f : 0.3f;

        // UIPanel 的 DrawSelf 内容
        if (_needsTextureLoading) {
            _needsTextureLoading = false;
            LoadTextures();
        }

        if (_backgroundTexture != null)
            DrawPanel(spriteBatch, _backgroundTexture.Value, BackgroundColor * opacity);

        if (_borderTexture != null)
            DrawPanel(spriteBatch, _borderTexture.Value, BorderColor * opacity);

        _modIcon.Color = Color.White * opacity;

        // Hover text
        if (!IsMouseHovering) return;

        if (_keyImage?.IsMouseHovering is true) {
            Main.instance.MouseText(Language.GetTextValue("tModLoader.ModStableOnPreviewWarning"));
            return;
        }

        if (updatedModDot?.IsMouseHovering is true) {
            Main.instance.MouseText(previousVersionHint == null ? Language.GetTextValue("tModLoader.ModAddedSinceLastLaunchMessage") : Language.GetTextValue("tModLoader.ModUpdatedSinceLastLaunchMessage", previousVersionHint));
            return;
        }

        string text = _mod.DisplayName + " v" + _mod.modFile.Version;

        // Author(s)
        if (_mod?.properties.author.Length > 0) {
            text += "\n" + Language.GetTextValue("tModLoader.ModsByline", _mod.properties.author);
        }

        // Mod is server side
        if (_mod?.properties.side is ModSide.Server) {
            text += "\n" + Language.GetTextValue("tModLoader.ModIsServerSide");
        }

        // Reload Required
        if (_mod?.properties.side != ModSide.Server && (_mod?.Enabled != _loaded || _configChangesRequireReload)) {
            text +=
                $"\n[c/FF6666:{(_configChangesRequireReload ? Language.GetTextValue("tModLoader.ModReloadForced") : Language.GetTextValue("tModLoader.ModReloadRequired"))}]";
        }

        // Config
        if (ModLoader.TryGetMod(ModName, out var loadedMod) && ConfigManager.Configs.ContainsKey(loadedMod)) {
            text += "\n" + Language.GetTextValue("Mods.ConciseModList.ModsOpenConfig");
        }

        // References
        if (_mod != null && _modReferences.Length > 0 && (!_mod.Enabled || _mod?.Enabled != _loaded)) {
            string refs = string.Join(", ", _mod.properties.modReferences);
            
            // remove the (click to enable) part in all languages
            string outputString = Language.GetTextValue("tModLoader.ModDependencyClickTooltip", refs);
            int index = outputString.IndexOf("\n", StringComparison.Ordinal);

            if (index >= 0) {
                outputString = outputString[..index];
            }

            text += $"\n{outputString}";
            text += "\n" + Language.GetTextValue("Mods.ConciseModList.Dependencies");
        }

        // More Info
        text += "\n" + Language.GetTextValue("Mods.ConciseModList.ModsMoreInfo");
        if (Main.keyState.PressingControl()) ShowMoreInfo(new UIMouseEvent(this, Main.MouseScreen), this);

        // Delete
        if (!_loaded) {
            text += "\n" + Language.GetTextValue("Mods.ConciseModList.Delete");
        }

        text = FontAssets.MouseText.Value.CreateWrappedText(text, Main.screenWidth * 0.5f);

        UICommon.TooltipMouseText(text);
    }
}