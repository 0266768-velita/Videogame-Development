using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports textures under Resources/UI and Resources/Sprites as crisp, uncompressed pixel art.
/// (The first import is fixed by MictlanSetupTool.ConfigureTextures; this keeps later imports consistent.)
/// </summary>
public class MenuTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool ui = assetPath.Contains("/Resources/UI/");
        bool sprites = assetPath.Contains("/Resources/Sprites/");
        if (!ui && !sprites) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 2048;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        if (sprites) ti.isReadable = !assetPath.EndsWith("ground.png") && !assetPath.Contains("water_");
    }
}
