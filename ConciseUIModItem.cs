﻿using System;
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

        _modReferences = _mod.properties.modReferences.Select(x => x.mod).ToArray();

        if (ModOrganizer.CheckStableBuildOnPreview(_mod)) {
            _keyImage = new UIHoverImage(Main.Assets.Request<Texture2D>(TextureAssets.Item[ItemID.LavaSkull].Name),
                Language.GetTextValue("tModLoader.ModStableOnPreviewWarning"));

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

        OnLeftClick += (e, _) => { _uiModStateText.LeftClick(e); };

        if (!_loaded) {
            OnMiddleClick += QuickModDelete;
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

        var innerDimensions = GetInnerDimensions();
        Vector2 drawPos = new Vector2(innerDimensions.X + 10f + _modIconAdjust, innerDimensions.Y + 45f);

        _modIcon.Color = Color.White * opacity;

        // Hover text
        if (!IsMouseHovering || _keyImage?.IsMouseHovering is true) return;

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