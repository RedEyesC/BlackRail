using UnityEditor;
using UnityEngine;

namespace GameEditor.AssetsCheckEditor
{
    public class MipmapGeneratorEditor
    {
        public static Texture2D[] fillrateSourceTextures;
        public static Color[] fillrateSourceColors;

        public const int maxMipLimit = 7;
        public const string sampleTexturePath = "Assets/Editor/AssetsCheckEditor/Atlas/";
        public const string exportTexturePath = "Assets/Editor/AssetsCheckEditor/Generated/";
        public const string exportTextureName = "MipmapForCheck";

        //参考https://www.lfzxb.top/projects-texel-density/

        [MenuItem("Tools/CreateMipmapGenerator", false)]
        public static void CreateMipmapGenerator()
        {
            //"mipmin为64，mipmax为8192，对应值为0-7"
            int maxMipLevel = 4;

            int levelCount = maxMipLevel + 1;

            if (fillrateSourceTextures == null || fillrateSourceTextures.Length != levelCount)
            {
                fillrateSourceTextures = new Texture2D[maxMipLimit + 1];
                fillrateSourceTextures[0] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "8K.png");
                fillrateSourceTextures[1] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "4K.png");
                fillrateSourceTextures[2] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "2K.png");
                fillrateSourceTextures[3] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "1K.png");
                fillrateSourceTextures[4] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "512.png");
                fillrateSourceTextures[5] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "256.png");
                fillrateSourceTextures[6] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "128.png");
                fillrateSourceTextures[7] = AssetDatabase.LoadAssetAtPath<Texture2D>(sampleTexturePath + "64.png");
            }

            if (fillrateSourceColors == null || fillrateSourceColors.Length != levelCount)
            {
                fillrateSourceColors = new Color[maxMipLimit + 1];
                fillrateSourceColors[0] = Color.red;
                fillrateSourceColors[1] = Color.green;
                fillrateSourceColors[2] = Color.blue;
                fillrateSourceColors[3] = Color.yellow;
                fillrateSourceColors[4] = Color.magenta;
                fillrateSourceColors[5] = Color.cyan;
                fillrateSourceColors[6] = Color.gray;
                fillrateSourceColors[7] = Color.white;
            }

            int resolution = Mathf.FloorToInt(Mathf.Pow(2, maxMipLevel)) * 64;

            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, levelCount, true);

            // 填充从mipmax到mip0
            for (int i = 0; i < maxMipLevel + 1; i++)
            {
                int width = resolution >> i;
                int height = resolution >> i;

                Texture2D sourcePatternTexture = fillrateSourceTextures[i + maxMipLimit - maxMipLevel];
                int sourcePatternTextureWidth = sourcePatternTexture.width;
                int sourcePatternTextureHeight = sourcePatternTexture.height;
                Color fillColor = fillrateSourceColors[i + maxMipLimit - maxMipLevel];

                Color[] texCol = sourcePatternTexture.GetPixels(0, 0, sourcePatternTextureWidth, sourcePatternTextureHeight);

                for (int p = 0; p < texCol.Length; ++p)
                {
                    var col = texCol[p];
                    col *= fillColor;
                    texCol[p] = col;
                }

                int copyStepX = width / sourcePatternTextureWidth;
                int copyStepY = height / sourcePatternTextureHeight;

                for (int x = 0; x < copyStepX; ++x)
                {
                    for (int y = 0; y < copyStepY; ++y)
                    {
                        texture.SetPixels(
                            x * sourcePatternTextureWidth,
                            y * sourcePatternTextureHeight,
                            sourcePatternTextureWidth,
                            sourcePatternTextureHeight,
                            texCol,
                            i
                        );
                    }
                }
            }

            texture.Apply(false);

            string textureName = exportTextureName + "_" + resolution + ".asset";

            AssetDatabase.CreateAsset(texture, exportTexturePath + textureName);

            AssetDatabase.Refresh();
        }
    }
}
