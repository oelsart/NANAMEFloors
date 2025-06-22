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
            if (!(baseTerrain.burnedDef != null && coverTerrain.burnedDef != null))
            {
                newTerr.burnedDef = BlendInner(new TerrainMask(terrainMask.maskTextureName, baseTerrain.burnedDef ?? baseTerrain, coverTerrain.burnedDef ?? coverTerrain));
            }
            var bluePrintDef = NewBlueprintDef_Terrain(newTerr, false);
            bluePrintDef.shortHash = 0;
            GiveShortHash(bluePrintDef, typeof(ThingDef), takenHashesPerDeftype[typeof(ThingDef)]);
            DefGenerator.AddImpliedDef(bluePrintDef);
            var frameDef = NewFrameDef_Terrain(newTerr, false);
            frameDef.shortHash = 0;
            GiveShortHash(frameDef, typeof(ThingDef), takenHashesPerDeftype[typeof(ThingDef)]);
            DefGenerator.AddImpliedDef(frameDef);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                GraphicRequest req = new GraphicRequest(typeof(Graphic_Terrain), baseTerrain.texturePath, NAF_DefOf.TerrainHardBlend.Shader, Vector2.one, baseTerrain.DrawColor, coverTerrain.DrawColor, null, 0, null, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);
                req.renderQueue = ((req.renderQueue == 0 && req.graphicData != null) ? req.graphicData.renderQueue : req.renderQueue);
                newTerr.graphic = new Graphic_Terrain();
                newTerr.graphic.Init(req);
                newTerr.graphic.MatSingle.SetTexture("_MainTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.texturePath));
                newTerr.graphic.MatSingle.GetTexture("_MainTex").filterMode = FilterMode.Point;
                newTerr.graphic.MatSingle.GetTexture("_MainTexTwo").filterMode = FilterMode.Point;
                if (!ModsConfig.BiotechActive) return;
                Shader shader = baseTerrain.pollutionShaderType == ShaderTypeDefOf.TerrainFadeRoughLinearAdd ? NAF_DefOf.TerrainFadeRoughLinearAddBlend.Shader : NAF_DefOf.TerrainHardLinearBurnBlend.Shader;
                string path = baseTerrain.pollutedTexturePath ?? baseTerrain.texturePath;
                newTerr.graphicPolluted = GraphicDatabase.Get(typeof(Graphic_Terrain), path, shader, Vector2.one, baseTerrain.DrawColor, coverTerrain.DrawColor, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);
                var matSingle = newTerr.graphicPolluted.MatSingle;
                if (!coverTerrain.pollutionOverlayTexturePath.NullOrEmpty())
                {
                    matSingle.SetTexture("_BurnTex", ContentFinder<Texture2D>.Get(baseTerrain.pollutionOverlayTexturePath, true));
                }
                matSingle.SetColor("_BurnColor", baseTerrain.pollutionColor);
                matSingle.SetVector("_ScrollSpeed", baseTerrain.pollutionOverlayScrollSpeed);
                matSingle.SetVector("_BurnScale", baseTerrain.pollutionOverlayScale);
                matSingle.SetColor("_PollutionTintColor", baseTerrain.pollutionTintColor);

                matSingle.SetTexture("_MainTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.pollutedTexturePath ?? coverTerrain.texturePath));
                if (!coverTerrain.pollutionOverlayTexturePath.NullOrEmpty()) matSingle.SetTexture("_BurnTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.pollutionOverlayTexturePath));
                matSingle.SetColor("_BurnColorTwo", coverTerrain.pollutionColor);
                matSingle.SetColor("_PollutionTintColorTwo", coverTerrain.pollutionTintColor);
                if (shader == NAF_DefOf.TerrainFadeRoughLinearAddBlend.Shader)
                {
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
                else field.SetValue(newTerr, field.GetValue(coverTerrain));
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

        private readonly static Action<Def, Type, HashSet<ushort>> GiveShortHash = (Action<Def, Type, HashSet<ushort>>)AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").CreateDelegate(typeof(Action<Def, Type, HashSet<ushort>>));

        private readonly static Func<TerrainDef, bool, ThingDef> NewBlueprintDef_Terrain = (Func<TerrainDef, bool, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, bool, ThingDef>));

        private readonly static Func<TerrainDef, bool, ThingDef> NewFrameDef_Terrain = (Func<TerrainDef, bool, ThingDef>)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Terrain").CreateDelegate(typeof(Func<TerrainDef, bool, ThingDef>));

        private readonly static Dictionary<Type, HashSet<ushort>> takenHashesPerDeftype = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
    }
}
