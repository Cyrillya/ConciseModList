using System;
using System.Collections.Generic;
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
    private UIImage _deprecatedIcon;

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
        SetIconImage();

        _modName = new UIText("Unavailable") {
            Left = new StyleDimension(_modIconAdjust, 0f),
            Top = { Pixels = 114514 }
        };

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
            if (ConciseModConfig.Instance.ConfigButton) {
                _configButton = new UIImage(ModAsset.ConfigButton) {
                    Width = {Pixels = 32},
                    Height = {Pixels = 32},
                    Left = {Pixels = -32, Precent = 1f},
                    Top = {Pixels = -32, Precent = 1f}
                };
                _configButton.OnLeftClick += OpenConfig;
                Append(_configButton);
            }

            if (ConfigManager.ModNeedsReload(loadedMod)) {
                _configChangesRequireReload = true;
            }
        }

        if (ModOrganizer.CheckStableBuildOnPreview(_mod) && ConciseModConfig.Instance.ObsidianSkull) {
            _keyImage = new UIHoverImage(Main.Assets.Request<Texture2D>(TextureAssets.Item[ItemID.LavaSkull].Name),
                "");

            Append(_keyImage);
        }

        switch (_mod.location) {
            case ModLocation.Workshop when ConciseModConfig.Instance.SteamIcon:
                var steamIcon = new UIImage(TextureAssets.Extra[243]) {
                    Left = { Pixels = -22, Percent = 1f }
                };
                Append(steamIcon);
                break;
            case ModLocation.Modpack when ConciseModConfig.Instance.ModpackIcon:
                var modpackIcon = new UIImage(UICommon.ModLocationModPackIcon) {
                    Left = { Pixels = -22, Percent = 1f }
                };
                Append(modpackIcon);
                break;
        }

        if (loadedMod != null) {
            _loaded = true;
        }

        if (ConciseModConfig.Instance.UpdateDot) {
            var oldModVersionData =
                ModOrganizer.modsThatUpdatedSinceLastLaunch.FirstOrDefault(x => x.ModName == ModName);
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
        }

        _modReferences = _mod.properties.modReferences.Select(x => x.mod).ToArray();

        var availableMods = ModOrganizer.RecheckVersionsToLoad();
        _modRequiresTooltip = "";
        HashSet<string> allDependencies = new();
        GetDependencies(_mod.Name, allDependencies);
        _modDependencies = allDependencies.ToArray();
        if (_modDependencies.Any()) {
            string refs = string.Join("\n",
                _modDependencies.Select(x =>
                    "- " + (Interface.modsMenu.FindUIModItem(x)?._mod.DisplayName ??
                            x + Language.GetTextValue("tModLoader.ModPackMissing"))));
            _modRequiresTooltip += Language.GetTextValue("tModLoader.ModDependencyTooltip", refs);
        }

        void GetDependencies(string modName, HashSet<string> allDependencies) {
            var modItem = Interface.modsMenu.FindUIModItem(modName);
            if (modItem == null)
                return; // Mod isn't downloaded, can't determine further recursive dependencies

            var dependencies = modItem._mod.properties.modReferences.Select(x => x.mod).ToArray();
            foreach (var dependency in dependencies) {
                if (allDependencies.Add(dependency)) {
                    GetDependencies(dependency, allDependencies);
                }
            }
        }

        HashSet<string> allDependents = new();
        GetDependents(_mod.Name, allDependents);
        _modDependents = allDependents.ToArray();
        if (_modDependents.Any()) {
            if (!string.IsNullOrWhiteSpace(_modRequiresTooltip))
                _modRequiresTooltip += "\n\n";
            string refs = string.Join("\n",
                _modDependents.Select(x =>
                    "- " + (Interface.modsMenu.FindUIModItem(x)?._mod.DisplayName ??
                            x + Language.GetTextValue("tModLoader.ModPackMissing"))));
            _modRequiresTooltip += Language.GetTextValue("tModLoader.ModDependentsTooltip", refs);
        }

        void GetDependents(string modName, HashSet<string> allDependents) {
            var dependents = availableMods
                .Where(m => m.properties.RefNames(includeWeak: false).Any(refName => refName.Equals(modName)))
                .Select(m => m.Name).ToArray();
            foreach (var dependent in dependents) {
                if (allDependents.Add(dependent))
                    GetDependents(dependent, allDependents);
            }
        }

        SubscribeEvents();

        int leftPixels = 2;
        if (!string.IsNullOrWhiteSpace(_modRequiresTooltip) && ConciseModConfig.Instance.ModReferenceIcon) {
            var icon = UICommon.ButtonDepsTexture;
            _modReferenceIcon = new UIImage(icon) {
                Left = {Pixels = leftPixels, Precent = 0f},
                Top = {Pixels = -24, Precent = 1f}
            };
            leftPixels += 24;

            Append(_modReferenceIcon);
        }

        if (_mod.properties.RefNames(true).Any() && _mod.properties.translationMod &&
            ConciseModConfig.Instance.TranslationModIcon) {
            var icon = UICommon.ButtonTranslationModTexture;
            _translationModIcon = new UIImage(icon) {
                Left = {Pixels = leftPixels, Precent = 0f},
                Top = {Pixels = -24, Precent = 1f}
            };
            Append(_translationModIcon);
        }

        CheckTModLoaderVersion();
    }

    private string GetUpdateVersion(out string updateURL) {
        string updateVersion = null;
        updateURL = "https://github.com/tModLoader/tModLoader/wiki/tModLoader-guide-for-players#beta-branches";

        // Detect if it's for a preview version ahead of our time
        if (BuildInfo.tMLVersion.MajorMinorBuild() < _mod.tModLoaderVersion.MajorMinorBuild()) {
            updateVersion = $"v{_mod.tModLoaderVersion}";

            if (_mod.tModLoaderVersion.MajorMinor() > BuildInfo.stableVersion)
                updateVersion = $"Preview {updateVersion}";
        }

        // Detect if it's for a different browser version entirely
        if (!CheckIfPublishedForThisBrowserVersion(out var modBrowserVersion)) {
            updateVersion = $"{modBrowserVersion} v{_mod.tModLoaderVersion}";
        }

        return updateVersion;
    }

    private void CheckTModLoaderVersion() {
        if (GetUpdateVersion(out var updateURL) != null) {
            _deprecatedIcon = new UIImage(ModAsset.Deprecated) {
                HAlign = 0.5f,
                VAlign = 0.5f
            };
            _deprecatedIcon.OnLeftClick += (a, b) => { Utils.OpenToURL(updateURL); };
            Append(_deprecatedIcon);
        }
    }

    public void SubscribeEvents() {
        OnLeftClick += LeftClickEvent;

        if (!_loaded && ModOrganizer.CanDeleteFrom(_mod.location)) {
            OnRightClick += QuickModDelete;
        }
    }

    public void LeftClickEvent(UIMouseEvent evt, UIElement listeningelement) {
        if (HoveringOnAnyElement(true)) return;
        _uiModStateText.LeftClick(evt);
    }

    public void SetIconImage() {
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
    }

    public override void DrawSelf(SpriteBatch spriteBatch) {
        ManageDrawing(spriteBatch);
        ManageHover();
    }

    public void ManageDrawing(SpriteBatch spriteBatch) {
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
    }

    private bool HoveringOnAnyElement(bool checkOnly) {
        if (DeprecatedIconHover(checkOnly))
            return true;
        if (KeyImageHover(checkOnly))
            return true;
        if (UpdatedDotHover(checkOnly))
            return true;
        if (ConfigButtonHover(checkOnly))
            return true;
        if (IndicatorIconHover(checkOnly))
            return true;
        return false;
    }

    public void ManageHover() {
        // Hover text
        if (!IsMouseHovering) return;

        if (HoveringOnAnyElement(false))
            return;

        ModInfoHoverText();
    }

    private bool DeprecatedIconHover(bool checkOnly) {
        if (_deprecatedIcon?.IsMouseHovering is not true) return false;
        if (checkOnly) return true;

        string updateVersion = GetUpdateVersion(out _);
        if (updateVersion is null) return false; // Shouldn't happen

        Main.instance.MouseText(Language.GetTextValue("tModLoader.MBRequiresTMLUpdate", updateVersion));
        return true;
    }

    public bool KeyImageHover(bool checkOnly) {
        if (_keyImage?.IsMouseHovering is not true) return false;
        if (checkOnly) return true;

        Main.instance.MouseText(Language.GetTextValue("tModLoader.ModStableOnPreviewWarning"));
        return true;
    }

    public bool UpdatedDotHover(bool checkOnly) {
        if (updatedModDot?.IsMouseHovering is not true) return false;
        if (checkOnly) return true;

        Main.instance.MouseText(previousVersionHint == null
            ? Language.GetTextValue("tModLoader.ModAddedSinceLastLaunchMessage")
            : Language.GetTextValue("tModLoader.ModUpdatedSinceLastLaunchMessage", previousVersionHint));
        return true;
    }

    public bool ConfigButtonHover(bool checkOnly) {
        if (_configButton?.IsMouseHovering is not true) return false;
        if (checkOnly) return true;

        Main.instance.MouseText(Language.GetTextValue("tModLoader.ModsOpenConfig"));
        return true;
    }

    public bool IndicatorIconHover(bool checkOnly) {
        bool hoveringOnOne = false;

        if (_modReferenceIcon?.IsMouseHovering == true) {
            if (checkOnly) return true;
            UICommon.TooltipMouseText(_modRequiresTooltip);
            hoveringOnOne = true;
        }

        if (_translationModIcon?.IsMouseHovering == true) {
            if (checkOnly) return true;
            string refs =
                string.Join(", ", _mod.properties.RefNames(true)); // Translation mods can be strong or weak references.
            UICommon.TooltipMouseText(Language.GetTextValue("tModLoader.TranslationModTooltip", refs));
            hoveringOnOne = true;
        }

        return hoveringOnOne;
    }

    public void ModInfoHoverText() {
        var infoLines = MakeModInfoLines();
        var text = "";
        for (var i = 0; i < infoLines.Count; i++) {
            var (_, line) = infoLines[i];
            if (i >= 1)
                text += "\n";
            text += line;
        }

        text = FontAssets.MouseText.Value.CreateWrappedText(text, Main.screenWidth * 0.5f);

        UICommon.TooltipMouseText(text);
    }

    /// <summary>
    /// Made for more convenient IL Editing and Detours <br/>
    /// Syntax: (Name, Text)
    /// </summary>
    public List<(string, string)> MakeModInfoLines() {
        List<(string, string)> lines = [
            ("DisplayName", $"{_mod.DisplayName} v{_mod.modFile.Version}")
        ];

        // Author(s)
        if (_mod?.properties.author.Length > 0) {
            lines.Add(("Authors", Language.GetTextValue("tModLoader.ModsByline", _mod.properties.author)));
        }

        // Mod is server side
        if (_mod?.properties.side is ModSide.Server) {
            lines.Add(("ServerSide", Language.GetTextValue("tModLoader.ModIsServerSide")));
        }

        // Reload Required
        if (_mod?.properties.side != ModSide.Server && (_mod?.Enabled != _loaded || _configChangesRequireReload)) {
            lines.Add(("ReloadRequired", $"[c/FF6666:{(_configChangesRequireReload
                ? Language.GetTextValue("tModLoader.ModReloadForced")
                : Language.GetTextValue("tModLoader.ModReloadRequired"))}]"));
        }

        // Config
        if (ModLoader.TryGetMod(ModName, out var loadedMod) && ConfigManager.Configs.ContainsKey(loadedMod)) {
            lines.Add(("OpenConfig", Language.GetTextValue("Mods.ConciseModList.ModsOpenConfig")));
        }

        // References
        if (_mod != null && _modReferences.Length > 0 && (!_mod.Enabled || _mod?.Enabled != _loaded)) {
            string refs = string.Join(", ", _mod.properties.modReferences);

            // remove the (click to enable) part in all languages
            string outputString = Language.GetTextValue("tModLoader.ModDependencyTooltip", refs);
            int index = outputString.IndexOf("\n", StringComparison.Ordinal);

            if (index >= 0) {
                outputString = outputString[..index];
            }

            lines.Add(("References", outputString));
        }

        // More Info
        lines.Add(("MoreInfo", Language.GetTextValue("Mods.ConciseModList.ModsMoreInfo")));
        if (Main.keyState.PressingControl()) ShowMoreInfo(new UIMouseEvent(this, Main.MouseScreen), this);

        // Delete
        if (!_loaded) {
            lines.Add(("Delete", Language.GetTextValue("Mods.ConciseModList.Delete")));
        }

        return lines;
    }
}