using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    [StaticConstructorOnStartup]
    public static class AddedShaders
    {
        public static Shader LoadShader(string shaderPath)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Shader>();
            }
            if (!lookup.ContainsKey(shaderPath))
            {
                lookup[shaderPath] = NanameFloors.content.assetBundles.loadedAssetBundles.Find(a => a.name == "nanamefloors").LoadAsset<Shader>($"Assets/Data/NanameFloors/{shaderPath}.shader");
            }
            Shader shader = lookup[shaderPath];

            if (shader == null)
            {
                Log.Warning("Could not load shader " + shaderPath);
                return ShaderDatabase.DefaultShader;
            }
            return shader;
        }

        public static Shader Invert(Shader shader)
        {
            if (shader == TerrainHardBlend) return TerrainHardBlendInvert;
            if (shader == TerrainHardBlendInvert) return TerrainHardBlend;
            if (shader == TerrainHardPollutedBlend) return TerrainHardPollutedBlendInvert;
            if (shader == TerrainHardPollutedBlendInvert) return TerrainHardPollutedBlend;
            if (shader == TerrainFadeRoughLinearAddBlend) return TerrainFadeRoughLinearAddBlendInvert;
            if (shader == TerrainFadeRoughLinearAddBlendInvert) return TerrainFadeRoughLinearAddBlend;
            Log.Error($"[NANAMEFloors] Could not get invert shader for {shader}");
            return null;
        }

        public static readonly Shader TerrainHardBlend = AddedShaders.LoadShader("TerrainHardBlend");

        public static readonly Shader TerrainHardBlendInvert = AddedShaders.LoadShader("TerrainHardBlendInvert");

        public static readonly Shader TerrainHardPollutedBlend = AddedShaders.LoadShader("TerrainHardLinearBurnBlend");

        public static readonly Shader TerrainHardPollutedBlendInvert = AddedShaders.LoadShader("TerrainHardLinearBurnBlendInvert");

        public static readonly Shader TerrainFadeRoughLinearAddBlend = AddedShaders.LoadShader("TerrainFadeRoughLinearAddBlend");

        public static readonly Shader TerrainFadeRoughLinearAddBlendInvert = AddedShaders.LoadShader("TerrainFadeRoughLinearAddBlendInvert");

        private static Dictionary<string, Shader> lookup = (Dictionary<string, Shader>)AccessTools.Field(typeof(ShaderDatabase), "lookup")?.GetValue(null);
    }
}
