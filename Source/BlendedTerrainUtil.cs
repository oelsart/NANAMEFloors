using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

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
            var takenHashes = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
            var bluePrintDef = (ThingDef)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewBlueprintDef_Terrain").Invoke(typeof(ThingDefGenerator_Buildings), new object[] { newTerr, false });
            bluePrintDef.shortHash = 0;
            AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").Invoke(typeof(ShortHashGiver), new object[] { bluePrintDef, typeof(ThingDef), takenHashes[typeof(ThingDef)] });
            DefGenerator.AddImpliedDef(bluePrintDef);
            var frameDef = (ThingDef)AccessTools.Method(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Terrain").Invoke(typeof(ThingDefGenerator_Buildings), new object[] { newTerr, false });
            frameDef.shortHash = 0;
            AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").Invoke(typeof(ShortHashGiver), new object[] { frameDef, typeof(ThingDef), takenHashes[typeof(ThingDef)] });
            DefGenerator.AddImpliedDef(frameDef);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                GraphicRequest req = new GraphicRequest(typeof(Graphic_Terrain), baseTerrain.texturePath, AddedShaders.TerrainHardBlend, Vector2.one, baseTerrain.DrawColor, coverTerrain.DrawColor, null, 0, null, "NanameFloors/TerrainMasks/" + terrainMask.maskTextureName);
                req.renderQueue = ((req.renderQueue == 0 && req.graphicData != null) ? req.graphicData.renderQueue : req.renderQueue);
                newTerr.graphic = new Graphic_Terrain();
                newTerr.graphic.Init(req);
                newTerr.graphic.MatSingle.SetTexture("_MainTexTwo", ContentFinder<Texture2D>.Get(coverTerrain.texturePath));
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
                else field.SetValue(newTerr, field.GetValue(coverTerrain));
            }
            newTerr.defName = $"{baseTerrain.defName}_{terrainMask.maskTextureName}_{coverTerrain.defName}";
            newTerr.label = coverTerrain.label + "NAF.and".Translate() + baseTerrain.label;
            var costList = new List<ThingDefCountClass>();
            if (baseTerrain.CostList != null)
            {
                costList.AddRange(baseTerrain.CostList);
            }
            if (coverTerrain.CostList != null)
            {
                costList.AddRange(coverTerrain.CostList);
            }
            newTerr.costList = costList.Select(c => new ThingDefCountClass(c.thingDef, Mathf.CeilToInt(c.count / 2f))).ToList();
            newTerr.statBases = null;
            coverTerrain.statBases.ForEach(s => newTerr.SetStatBaseValue(s.stat, (s.value + baseTerrain.GetStatValueAbstract(s.stat)) / 2));
            newTerr.shortHash = 0;
            var takenHashes = AccessTools.StaticFieldRefAccess<Dictionary<Type, HashSet<ushort>>>(typeof(ShortHashGiver), "takenHashesPerDeftype");
            AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash").Invoke(typeof(ShortHashGiver), new object[] { newTerr, typeof(TerrainDef), takenHashes[typeof(TerrainDef)] });
            newTerr.modContentPack = NanameFloors.content;
            return newTerr;
        }
    }
}
