using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NanameFloors;

public static class BlendedTerrainUtil
{
    private static readonly Action<Def, Type, HashSet<ushort>> GiveShortHash = (Action<Def, Type, HashSet<ushort>>)AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").CreateDelegate(typeof(Action<Def, Type, HashSet<ushort>>));
    private static readonly Func<TerrainDef, ThingDef> NewBlueprintDef_Terrain = (Func<TerrainDef, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, ThingDef>));
    private static readonly Func<TerrainDef, ThingDef> NewFrameDef_Terrain = (Func<TerrainDef, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, ThingDef>));
    private static readonly Dictionary<Type, HashSet<ushort>> takenHashesPerDeftype = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
 
    public static void MakeBlendedTerrain(TerrainMask terrainMask)
    {
        var baseTerrain = terrainMask.baseTerrain;
        var coverTerrain = terrainMask.coverTerrain;
        var newTerr = BlendInner(terrainMask);
        if (baseTerrain.burnedDef != null)
        {
            newTerr.burnedDef = BlendInner(new TerrainMask(terrainMask.maskTextureName, baseTerrain.burnedDef, coverTerrain.burnedDef ?? coverTerrain, terrainMask.rotation ?? Rot4.North));
            newTerr.burnedDef.graphic = baseTerrain.burnedDef.graphic;
            newTerr.burnedDef.graphicPolluted = baseTerrain.burnedDef.graphicPolluted;
            newTerr.burnedDef.PostLoad();
        }
        var bluePrintDef = NewBlueprintDef_Terrain(newTerr);
        bluePrintDef.shortHash = 0;
        GiveShortHash(bluePrintDef, typeof(ThingDef), takenHashesPerDeftype[typeof(ThingDef)]);
        DefGenerator.AddImpliedDef(bluePrintDef);
        var frameDef = NewFrameDef_Terrain(newTerr);
        frameDef.shortHash = 0;
        GiveShortHash(frameDef, typeof(ThingDef), takenHashesPerDeftype[typeof(ThingDef)]);
        DefGenerator.AddImpliedDef(frameDef);

        if (baseTerrain.graphic == BaseContent.BadGraphic)
        {
            baseTerrain.PostLoad();
        }

        newTerr.modExtensions = [terrainMask];
        DefGenerator.AddImpliedDef(newTerr);

        if (newTerr.dominantStyleCategory != null && Find.World != null)
        {
            foreach (var ideo in Find.IdeoManager.IdeosListForReading)
            {
                ideo.RecachePossibleBuildables();
            }
        }
    }

    private static BlendedTerrainDef BlendInner(TerrainMask terrainMask)
    {
        var baseTerrain = terrainMask.baseTerrain;
        var coverTerrain = terrainMask.coverTerrain;
        var newTerr = new BlendedTerrainDef();
        foreach (var field in typeof(BlendedTerrainDef).GetFields())
        {
            switch (field.Name)
            {
                case "fertility":
                    field.SetValue(newTerr, Mathf.Min(baseTerrain.fertility, coverTerrain.fertility));
                    break;
                case "graphic":
                    newTerr.graphic = baseTerrain.graphic;
                    break;
                case "graphicPolluted":
                    newTerr.graphicPolluted = baseTerrain.graphicPolluted;
                    break;
                default:
                {
                    if (field.FieldType == typeof(float)) field.SetValue(newTerr, ((float)field.GetValue(baseTerrain) + (float)field.GetValue(coverTerrain)) / 2f);
                    else if (field.FieldType == typeof(int)) field.SetValue(newTerr, (int)Mathf.Round(((int)field.GetValue(baseTerrain) + (int)field.GetValue(coverTerrain)) / 2f));
                    else field.SetValue(newTerr, field.GetValue(coverTerrain));
                    break;
                }
            }
        }
        newTerr.defName = terrainMask.DefName;
        newTerr.label = coverTerrain.label + "NAF.and".Translate() + baseTerrain.label;
        newTerr.costList = [];
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
        foreach (var stat in coverTerrain.statBases)
        {
            newTerr.SetStatBaseValue(stat.stat, (stat.value + baseTerrain.GetStatValueAbstract(stat.stat)) / 2f);
        }
        newTerr.shortHash = 0;
        GiveShortHash(newTerr, typeof(TerrainDef), takenHashesPerDeftype[typeof(TerrainDef)]);
        newTerr.modContentPack = NanameFloors.content;
        newTerr.edgeType = TerrainDef.TerrainEdgeType.Hard;
        if (newTerr.dominantStyleCategory != null)
        {
            newTerr.dominantStyleCategory.addDesignators.Add(newTerr);
            ((List<BuildableDef>)AccessTools.Field(typeof(StyleCategoryDef), "cachedAllDesignatorBuildables").GetValue(newTerr.dominantStyleCategory))?.Add(newTerr);
        }
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
            return AddedShaders.TerrainHardBlend;
        }
        if (shader == ShaderDatabase.TerrainFade)
        {
            return AddedShaders.TerrainFadeBlend;
        }
        if (shader == ShaderDatabase.TerrainWater)
        {
            return AddedShaders.TerrainWaterBlend;
        }
        if (shader == ShaderDatabase.TerrainFadeRough)
        {
            return AddedShaders.TerrainFadeRoughBlend;
        }
        if (shader == ShaderTypeDefOf.TerrainFadeRoughLinearAdd.Shader)
        {
            return AddedShaders.TerrainFadeRoughLinearAddBlend;
        }
        if (shader == DefDatabase<ShaderTypeDef>.GetNamed("TerrainFadeRoughSoftLight", false)?.Shader)
        {
            return AddedShaders.TerrainFadeRoughSoftLightBlend;
        }
        if (shader == ShaderDatabase.LoadShader("Map/WaterDepth"))
        {
            return AddedShaders.WaterDepthBlend;
        }
        if (ModsConfig.BiotechActive)
        {
            if (shader == ShaderDatabase.TerrainHardPolluted)
            {
                return AddedShaders.TerrainHardLinearBurnBlend;
            }
            if (shader == ShaderDatabase.TerrainFadePolluted)
            {
                return AddedShaders.TerrainFadeLinearBurnBlend;
            }
            if (shader == ShaderDatabase.TerrainFadeRoughPolluted)
            {
                return AddedShaders.TerrainFadeRoughLinearBurnBlend;
            }
            if (shader == DefDatabase<ShaderTypeDef>.GetNamed("TerrainWaterPolluted", false)?.Shader)
            {
                return AddedShaders.TerrainWaterPollutedBlend;
            }
        }
        Log.Warning($"[NanameFloors] {shader.name} is unsupported terrain shader. Using TerrainHardBlend instead.");
        return AddedShaders.TerrainHardBlend;
    }

    public static bool IsAddedShader(Shader shader)
    {
        return shader == AddedShaders.TerrainHardBlend || shader == AddedShaders.TerrainFadeBlend || shader == AddedShaders.TerrainWaterBlend ||
               shader == AddedShaders.TerrainFadeRoughBlend || shader == AddedShaders.TerrainFadeRoughLinearAddBlend || shader == AddedShaders.TerrainFadeRoughSoftLightBlend ||
               shader == AddedShaders.WaterDepthBlend || shader == AddedShaders.TerrainHardLinearBurnBlend || shader == AddedShaders.TerrainFadeLinearBurnBlend ||
               shader == AddedShaders.TerrainFadeRoughLinearBurnBlend || shader == AddedShaders.TerrainWaterPollutedBlend;
    }

    extension(BuildableDef def)
    {
        public bool IsNanameSupported => def is TerrainDef { bridge: false };
    }
}