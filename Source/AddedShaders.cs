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
            Dictionary<string, Shader> lookup = AccessTools.StaticFieldRefAccess<Dictionary<string, Shader>>(typeof(ShaderDatabase), "lookup");
            if (lookup == null)
            {
                lookup = new Dictionary<string, Shader>();
            }
            if (!lookup.ContainsKey(shaderPath))
            {
                lookup[shaderPath] = NanameFloors.Bundle.LoadAsset<Shader>($"Assets/Data/NanameFloors/{shaderPath}.shader");
            }
            Shader shader = lookup[shaderPath];

            if (shader == null)
            {
                Log.Warning("Could not load shader " + shaderPath);
                return ShaderDatabase.DefaultShader;
            }
            return shader;
        }

        public static bool IsAddedShader(Shader shader)
        {
            return shader == TerrainHardBlend ||
                shader == TerrainFadeBlend ||
                shader == TerrainFadeRoughBlend ||
                shader == TerrainWaterBlend ||
                shader == TerrainWaterPollutedBlend ||
                shader == TerrainHardPollutedBlend ||
                shader == TerrainFadePollutedBlend ||
                shader == TerrainFadeRoughSoftLightBlend ||
                shader == TerrainFadeRoughLinearAddBlend ||
                shader == TerrainFadeRoughLinearBurnBlend;
        }

        public static readonly Shader TerrainHardBlend = LoadShader("TerrainHardBlend");

        public static readonly Shader TerrainFadeBlend = LoadShader("TerrainFadeBlend");

        public static readonly Shader TerrainFadeRoughBlend = LoadShader("TerrainFadeRoughBlend");

        public static readonly Shader TerrainWaterBlend = LoadShader("TerrainWaterBlend");

        public static readonly Shader TerrainWaterPollutedBlend = LoadShader("TerrainWaterPollutedBlend");

        public static readonly Shader TerrainHardPollutedBlend = LoadShader("TerrainHardLinearBurnBlend");

        public static readonly Shader TerrainFadePollutedBlend = LoadShader("TerrainFadeLinearBurnBlend");

        public static readonly Shader TerrainFadeRoughSoftLightBlend = LoadShader("TerrainFadeRoughSoftLightBlend");

        public static readonly Shader TerrainFadeRoughLinearAddBlend = LoadShader("TerrainFadeRoughLinearAddBlend");

        public static readonly Shader TerrainFadeRoughLinearBurnBlend = LoadShader("TerrainFadeRoughLinearBurnBlend");

        public static readonly Shader WaterDepthBlend = LoadShader("WaterDepthBlend");
    }
}
