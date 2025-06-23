using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public static class BlendedTerrainUtil
    {
        public static void MakeBlendedTerrain(TerrainMask terrainMask)
        {
            var baseTerrain = terrainMask.baseTerrain;
            var coverTerrain = terrainMask.coverTerrain;
            var newTerr = BlendInner(terrainMask);
            if (baseTerrain.burnedDef != null)
            {
                newTerr.burnedDef = BlendInner(new TerrainMask(terrainMask.maskTextureName, baseTerrain.burnedDef, coverTerrain.burnedDef ?? coverTerrain));
                newTerr.burnedDef.graphic = baseTerrain.burnedDef.graphic;
                newTerr.burnedDef.graphicPolluted = baseTerrain.burnedDef.graphicPolluted;
                newTerr.burnedDef.PostLoad();
            }
            var bluePrintDef = NewBlueprintDef_Terrain(newTerr, false);
            bluePrintDef.shortHash = 0;
            GiveShortHash(bluePrintDef, typeof(ThingDef), takenHashesPerDeftype[typeof(ThingDef)]);
            DefGenerator.AddImpliedDef(bluePrintDef);
            var frameDef = NewFrameDef_Terrain(newTerr, false);
            frameDef.shortHash = 0;
            GiveShortHash(frameDef, typeof(ThingDef), takenHashesPerDeftype[typeof(ThingDef)]);
            DefGenerator.AddImpliedDef(frameDef);

            if (baseTerrain.graphic == BaseContent.BadGraphic)
            {
                baseTerrain.PostLoad();
            }

            newTerr.modExtensions = new List<DefModExtension>() { terrainMask };
            DefGenerator.AddImpliedDef(newTerr);
        }

        private static BlendedTerrainDef BlendInner(TerrainMask terrainMask)
        {
            var baseTerrain = terrainMask.baseTerrain;
            var coverTerrain = terrainMask.coverTerrain;
            var newTerr = new BlendedTerrainDef();
            foreach (var field in typeof(BlendedTerrainDef).GetFields())
            {
                if (field.Name == "fertility") field.SetValue(newTerr, 0f);
                else if (field.FieldType == typeof(float)) field.SetValue(newTerr, ((float)field.GetValue(baseTerrain) + (float)field.GetValue(coverTerrain)) / 2f);
                else if (field.FieldType == typeof(int)) field.SetValue(newTerr, (int)Mathf.Round(((int)field.GetValue(baseTerrain) + (int)field.GetValue(coverTerrain)) / 2f));
                else field.SetValue(newTerr, field.GetValue(baseTerrain));
            }
            newTerr.defName = $"{baseTerrain.defName}_{terrainMask.maskTextureName}_{coverTerrain.defName}";
            newTerr.label = coverTerrain.label + "NAF.and".Translate() + baseTerrain.label;
            newTerr.costList = new List<ThingDefCountClass>();
            if (baseTerrain.CostList != null)
            {
                newTerr.costList.AddRange(baseTerrain.CostList);
            }
            if (coverTerrain.CostList != null)
            {
                newTerr.costList.AddRange(coverTerrain.CostList);
            }
            for (var i = 0; i < newTerr.costList.Count; i++)
            {
                newTerr.costList[i] = new ThingDefCountClass(newTerr.costList[i].thingDef, Mathf.CeilToInt(newTerr.costList[i].count / 2f));
            }
            newTerr.statBases = null;
            coverTerrain.statBases.ForEach(s => newTerr.SetStatBaseValue(s.stat, (s.value + baseTerrain.GetStatValueAbstract(s.stat)) / 2));
            newTerr.shortHash = 0;
            GiveShortHash(newTerr, typeof(TerrainDef), takenHashesPerDeftype[typeof(TerrainDef)]);
            newTerr.modContentPack = NanameFloors.content;
            return newTerr;
        }

        public static Shader GetBlendShader(this Shader shader)
        {
            if (shader == null)
            {
                return BaseContent.BadGraphic.Shader;
            }
            if (shader == ShaderDatabase.TerrainHard)
            {
                return NAF_DefOf.TerrainHardBlend.Shader;
            }
            if (shader == ShaderDatabase.TerrainFade)
            {
                return NAF_DefOf.TerrainFadeBlend.Shader;
            }
            if (shader == ShaderDatabase.TerrainWater)
            {
                return NAF_DefOf.TerrainWaterBlend.Shader;
            }
            if (shader == ShaderDatabase.TerrainFadeRough)
            {
                return NAF_DefOf.TerrainFadeRoughBlend.Shader;
            }
            if (shader == ShaderTypeDefOf.TerrainFadeRoughLinearAdd.Shader)
            {
                return NAF_DefOf.TerrainFadeRoughLinearAddBlend.Shader;
            }
            if (shader == DefDatabase<ShaderTypeDef>.GetNamed("TerrainFadeRoughSoftLight", false)?.Shader)
            {
                return NAF_DefOf.TerrainFadeRoughSoftLightBlend.Shader;
            }
            if (shader == ShaderDatabase.LoadShader("Map/WaterDepth"))
            {
                return NAF_DefOf.WaterDepthBlend.Shader;
            }
            if (ModsConfig.BiotechActive)
            {
                if (shader == ShaderDatabase.TerrainHardPolluted)
                {
                    return NAF_DefOf.TerrainHardLinearBurnBlend.Shader;
                }
                if (shader == ShaderDatabase.TerrainFadePolluted)
                {
                    return NAF_DefOf.TerrainFadeLinearBurnBlend.Shader;
                }
                if (shader == ShaderDatabase.TerrainFadeRoughPolluted)
                {
                    return NAF_DefOf.TerrainFadeRoughLinearBurnBlend.Shader;
                }
                if (shader == DefDatabase<ShaderTypeDef>.GetNamed("TerrainWaterPolluted", false)?.Shader)
                {
                    return NAF_DefOf.TerrainWaterPollutedBlend.Shader;
                }
            }
            Log.Warning($"[NanameFloors] {shader.name} is unsupported terrain shader. Using TerrainHardBlend instead.");
            return NAF_DefOf.TerrainHardBlend.Shader;
        }

        public static bool IsAddedShader(Shader shader)
        {
            return shader == NAF_DefOf.TerrainHardBlend.Shader || shader == NAF_DefOf.TerrainFadeBlend.Shader || shader == NAF_DefOf.TerrainWaterBlend.Shader ||
                shader == NAF_DefOf.TerrainFadeRoughBlend.Shader || shader == NAF_DefOf.TerrainFadeRoughLinearAddBlend.Shader || shader == NAF_DefOf.TerrainFadeRoughSoftLightBlend.Shader ||
                shader == NAF_DefOf.WaterDepthBlend.Shader || shader == NAF_DefOf.TerrainHardLinearBurnBlend.Shader || shader == NAF_DefOf.TerrainFadeLinearBurnBlend.Shader ||
                shader == NAF_DefOf.TerrainFadeRoughLinearBurnBlend.Shader || shader == NAF_DefOf.TerrainWaterPollutedBlend.Shader;
        }

        private readonly static Action<Def, Type, HashSet<ushort>> GiveShortHash = (Action<Def, Type, HashSet<ushort>>)AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").CreateDelegate(typeof(Action<Def, Type, HashSet<ushort>>));

        private readonly static Func<TerrainDef, bool, ThingDef> NewBlueprintDef_Terrain = (Func<TerrainDef, bool, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, bool, ThingDef>));

        private readonly static Func<TerrainDef, bool, ThingDef> NewFrameDef_Terrain = (Func<TerrainDef, bool, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, bool, ThingDef>));

        private readonly static Dictionary<Type, HashSet<ushort>> takenHashesPerDeftype = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
    }
}
