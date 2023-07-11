using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace ConciseModList;

public static class ModAsset
{
    private static AssetRepository _repo;

    static ModAsset() {
        _repo = ModLoader.GetMod("ConciseModList").Assets;
    }

    private const string ConfigButtonPath = @"ConfigButton";

    public static Asset<Texture2D> ConfigButton =>
        _repo.Request<Texture2D>(ConfigButtonPath, AssetRequestMode.ImmediateLoad);

    private const string ExportListButtonPath = @"ExportListButton";

    public static Asset<Texture2D> ExportListButton =>
        _repo.Request<Texture2D>(ExportListButtonPath, AssetRequestMode.ImmediateLoad);
}