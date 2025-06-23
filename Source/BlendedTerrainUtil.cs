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
                GraphicRequest req = new GraphicRequest(typeof(Graphic_Terrain), baseTerrain.texturePath, AddedShaders.TerrainHardBlend, Vector2.one, baseTerrain.DrawColor, coverTerrain.DrawColor, null, 0, null, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);
                req.renderQueue = ((req.renderQueue == 0 && req.graphicData != null) ? req.graphicData.renderQueue : req.renderQueue);
                newTerr.graphic = new Graphic_Terrain();
                newTerr.graphic.Init(req);
                newTerr.graphic.MatSingle.SetTexture("_MainTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.texturePath));
                newTerr.graphic.MatSingle.GetTexture("_MainTex").filterMode = FilterMode.Point;
                newTerr.graphic.MatSingle.GetTexture("_MainTexTwo").filterMode = FilterMode.Point;
                if (!ModsConfig.BiotechActive) return;
                Shader shader = baseTerrain.pollutionShaderType == ShaderTypeDefOf.TerrainFadeRoughLinearAdd ? AddedShaders.TerrainFadeRoughLinearAddBlend : AddedShaders.TerrainHardPollutedBlend;
                string path = baseTerrain.pollutedTexturePath.NullOrEmpty() ? baseTerrain.texturePath : baseTerrain.pollutedTexturePath;
                newTerr.graphicPolluted = GraphicDatabase.Get(typeof(Graphic_Terrain), path, shader, Vector2.one, baseTerrain.DrawColor, coverTerrain.DrawColor, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);
                var matSingle = newTerr.graphicPolluted.MatSingle;
                matSingle.SetTexture("_MainTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.pollutedTexturePath ?? coverTerrain.texturePath));
                if (!coverTerrain.pollutionOverlayTexturePath.NullOrEmpty()) matSingle.SetTexture("_BurnTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.pollutionOverlayTexturePath));
                matSingle.SetColor("_BurnColorTwo", coverTerrain.pollutionColor);
                matSingle.SetColor("_PollutionTintColorTwo", coverTerrain.pollutionTintColor);
                if (shader == AddedShaders.TerrainFadeRoughLinearAddBlend)
                {
                    matSingle.SetVector("_BurnScale", baseTerrain.pollutionOverlayScale);
                    matSingle.SetTexture("_AlphaAddTex", TexGame.AlphaAddTex);
                }
            });
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
                    return AddedShaders.TerrainHardPollutedBlend;
                }
                if (shader == ShaderDatabase.TerrainFadePolluted)
                {
                    return AddedShaders.TerrainFadePollutedBlend;
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

        private readonly static Action<Def, Type, HashSet<ushort>> GiveShortHash = (Action<Def, Type, HashSet<ushort>>)AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").CreateDelegate(typeof(Action<Def, Type, HashSet<ushort>>));

        private readonly static Func<TerrainDef, ThingDef> NewBlueprintDef_Terrain = (Func<TerrainDef, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, ThingDef>));

        private readonly static Func<TerrainDef, ThingDef> NewFrameDef_Terrain = (Func<TerrainDef, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, ThingDef>));

        private readonly static Dictionary<Type, HashSet<ushort>> takenHashesPerDeftype = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
    }
}
