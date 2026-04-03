using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SmashTools
{
    public class AssetBundleBuilder : MonoBehaviour
    {
        private const string OutputPath = "../../Shaders_1.5";

        private static readonly BuildTarget[] BuildTargets =
          { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneOSX, BuildTarget.StandaloneLinux64 };

        private static string PlatformSuffix(BuildTarget buildTarget)
        {
            switch(buildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    return "_win";
                case BuildTarget.StandaloneLinux64:
                    return "_linux";
                case BuildTarget.StandaloneOSX:
                    return "_mac";
                default:
                    throw new NotSupportedException(buildTarget.ToString());
            };
        }

        private static string[] GetAssetPaths<T>()
        {
            string[] guids =
              AssetDatabase.FindAssets($"t:{typeof(T).Name}",
                new[] { $"Assets/Data/NanameFloors" });

            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                paths[i] = path;
            }
            return paths;
        }

        [MenuItem("Assets/Build AssetBundles/Naname Floors")]
        private static void BuildAssetBundles()
        {
            if (!Directory.Exists(OutputPath))
                throw new DirectoryNotFoundException(OutputPath);

            BuildForMod();
        }

        public static void BuildForMod()
        {
            const string TextureBundleName = "oels_nanamefloors_textures";
            const string ShaderBundleName = "oels_nanamefloors_shaders";

            // Start fresh for build folder
            if (!Directory.Exists(OutputPath))
                throw new DirectoryNotFoundException(OutputPath);

            Directory.Delete(OutputPath, true);
            Directory.CreateDirectory(OutputPath);

            // Platform independent
            AssetBundleBuild[] bundles = new AssetBundleBuild[1];
            bundles[0].assetBundleName = TextureBundleName;
            bundles[0].assetNames = GetAssetPaths<Texture2D>();

            BuildPipeline.BuildAssetBundles(OutputPath, bundles,
              BuildAssetBundleOptions.ChunkBasedCompression,
              BuildTarget.StandaloneWindows64);


            // Platform dependent
            AssetBundleBuild[] platformBundles = new AssetBundleBuild[1];
            platformBundles[0].assetBundleName = ShaderBundleName;
            platformBundles[0].assetNames = GetAssetPaths<Shader>();

            BuildForPlatform(OutputPath, platformBundles,
              BuildAssetBundleOptions.ChunkBasedCompression);
        }

        private static void BuildForPlatform(string directoryPath, AssetBundleBuild[] bundles,
          BuildAssetBundleOptions bundleOptions)
        {
            foreach (BuildTarget buildTarget in BuildTargets)
            {
                AssetBundleBuild[] platformBundles =
                  new AssetBundleBuild[bundles.Length];
                for (int i = 0; i < bundles.Length; i++)
                {
                    AssetBundleBuild bundle = bundles[i];
                    AssetBundleBuild platformBundle = bundle;
                    platformBundle.assetBundleName = bundle.assetBundleName + PlatformSuffix(buildTarget);
                    platformBundles[i] = platformBundle;
                }
                BuildPipeline.BuildAssetBundles(directoryPath, platformBundles, bundleOptions, buildTarget);
            }
        }
    }
}