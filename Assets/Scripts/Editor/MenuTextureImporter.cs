using UnityEditor;
using UnityEngine;

/// <summary>Imports textures under Resources/UI as crisp, uncompressed pixel art.</summary>
public class MenuTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/UI/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 2048;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
