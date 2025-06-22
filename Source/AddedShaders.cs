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
            var lookup = AccessTools.FieldRefAccess<Dictionary<string, Shader>>(typeof(ShaderDatabase), "lookup")();
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

        public static readonly Shader TerrainHardBlend = AddedShaders.LoadShader("TerrainHardBlend");

        public static readonly Shader TerrainHardPollutedBlend = AddedShaders.LoadShader("TerrainHardLinearBurnBlend");

        public static readonly Shader TerrainFadeRoughLinearAddBlend = AddedShaders.LoadShader("TerrainFadeRoughLinearAddBlend");
    }
}
