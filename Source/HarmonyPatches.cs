using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.nanamefloors");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Designator_Build), "DesignateSingleCell")]
    [HarmonyBefore("Uuugggg.rimworld.Replace_Stuff.main")]
    public static class Patch_Designator_Build_DesignateSingleCell
    {
        public static void Prefix(IntVec3 c, ref BuildableDef ___entDef, Designator_Build __instance, ref BuildableDef __state)
        {
            __state = ___entDef;
            TerrainDef terrainDef;
            if (NanameFloors.UI.selectedMask != null && (terrainDef = ___entDef as TerrainDef) != null)
            {
                var maskTextureName = NanameFloors.UI.selectedMask.name;
                var baseTerr = c.GetTerrain(__instance.Map);
                if (baseTerr is BlendedTerrainDef) baseTerr = baseTerr.GetModExtension<TerrainMask>().baseTerrain;
                var defName = $"{baseTerr.defName}_{maskTextureName}_{terrainDef.defName}";
                if (DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(defName) == null)
                {
                    var terrainMask = new TerrainMask(maskTextureName, baseTerr, terrainDef);
                    BlendedTerrainUtil.MakeBlendedTerrain(terrainMask);
                }
                ___entDef = DefDatabase<BlendedTerrainDef>.GetNamed(defName);
            }
        }

        public static void Postfix(ref BuildableDef ___entDef, BuildableDef __state)
        {
            ___entDef = __state;
        }
    }

    //DubsMintMenusなどでDoExtraGuiControlsが呼ばれないことがあるので、DesignatorManagerから確実に実行されるDrawMouseAttachmentsにフックしてます
    [HarmonyPatch(typeof(Designator_Place), nameof(Designator_Place.DrawMouseAttachments))]
    public static class Patch_Designator_Place_DrawMouseAttachments
    {
        public static void Postfix(Designator_Place __instance)
        {
            if (__instance.PlacingDef is TerrainDef)
            {
                Find.WindowStack.ImmediateWindow(9359779, NanameFloors.UI.windowRect, WindowLayer.GameUI, () => NanameFloors.UI.DoWindowContents());
            }
        }
    }

    [HarmonyPatch(typeof(TerrainGrid), "ExposeTerrainGrid")]
    public static class TerrainGrid_ExposeTerrainGrid_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && c.operand as MethodInfo == AccessTools.PropertyGetter(typeof(DefDatabase<TerrainDef>), "AllDefs"));
            codes.Insert(pos + 1, CodeInstruction.Call(typeof(TerrainGrid_ExposeTerrainGrid_Patch), "ConcatDefs"));
            return codes;
        }

        public static IEnumerable<TerrainDef> ConcatDefs(IEnumerable<TerrainDef> terrainDefs)
        {
            return terrainDefs.Concat(DefDatabase<BlendedTerrainDef>.AllDefs);
        }
    }

    [HarmonyPatch(typeof(MaterialPool), "MatFrom", new Type[] { typeof(MaterialRequest) })]
    public static class Patch_MaterialPool_MatFrom
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var f_colorTwo = AccessTools.Field(typeof(MaterialRequest), "colorTwo");
            var pos = codes.FindIndex(c => c.StoresField(f_colorTwo)) + 1;
            var label = generator.DefineLabel();

            codes[pos].labels.Add(label);
            codes.InsertRange(pos, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(MaterialRequest), "shader"),
                CodeInstruction.Call(typeof(BlendedTerrainUtil), nameof(BlendedTerrainUtil.IsAddedShader)),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Patch_MaterialPool_MatFrom), nameof(ForceCreateMaterial)),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }

        public static Material ForceCreateMaterial(MaterialRequest req)
        {
            Material material = new Material(req.shader);
            material.name = req.shader.name;
            if (req.mainTex != null)
            {
                Material material2 = material;
                material2.name = material2.name + "_" + req.mainTex.name;
                material.mainTexture = req.mainTex;
            }
            material.color = req.color;
            if (req.maskTex != null)
            {
                material.SetTexture(ShaderPropertyIDs.MaskTex, req.maskTex);
            }
            if (req.renderQueue != 0)
            {
                material.renderQueue = req.renderQueue;
            }
            if (!req.shaderParameters.NullOrEmpty())
            {
                for (int i = 0; i < req.shaderParameters.Count; i++)
                {
                    req.shaderParameters[i].Apply(material);
                }
            }
            return material;
        }
    }

    [HarmonyPatch("Verse.SectionLayer_Terrain", "Regenerate")]
    public static class Patch_SectionLayer_Terrain_Regenerate
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var m_MoveNext = AccessTools.Method(typeof(CellRect.Enumerator), nameof(CellRect.Enumerator.MoveNext));
            var pos = codes.FindIndex(c => c.Calls(m_MoveNext));
            codes.InsertRange(pos, new[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadLocal(8),
                CodeInstruction.LoadLocal(6),
                CodeInstruction.Call(typeof(Patch_SectionLayer_Terrain_Regenerate), nameof(GenerateCover))
            });
            return codes;
        }

        public static void GenerateCover(SectionLayer instance, CellTerrain cellTerrain, IntVec3 intVec)
        {
            if (!(cellTerrain.def is BlendedTerrainDef blendedTerrainDef)) return;
            Material GetMaterial()
            {
                if (SectionLayer_Watergen.IsAssignableFrom(instance.GetType()))
                {
                    return blendedTerrainDef.CoverWaterDepthMaterial;
                }

                var coverTerrain = blendedTerrainDef.CoverTerrain;
                var polluted = cellTerrain.polluted && cellTerrain.snowCoverage < 0.4f && cellTerrain.sandCoverage < 0.4f && blendedTerrainDef.CoverGraphicPolluted != BaseContent.BadGraphic;
                var color = cellTerrain.color;
                var key = (coverTerrain, polluted, color, blendedTerrainDef.MaskTex);
                if (!terrainMatCache.ContainsKey(key))
                {
                    Graphic graphic = polluted ? blendedTerrainDef.CoverGraphicPolluted ?? blendedTerrainDef.CoverGraphic : blendedTerrainDef.CoverGraphic;
                    if (color != null)
                    {
                        terrainMatCache[key] = graphic.GetColoredVersion(graphic.Shader, color.color, Color.white).MatSingle;
                    }
                    else
                    {
                        terrainMatCache[key] = graphic.MatSingle;
                    }
                }

                return terrainMatCache[key];
            }

            bool AllowRenderingFor(TerrainDef terrain)
            {
                return DebugViewSettings.drawTerrainWater || !terrain.HasTag("Water");
            }
            LayerSubMesh subMesh = instance.GetSubMesh(blendedTerrainDef.CoverTerrain.dontRender ? MatBases.ShadowMask : GetMaterial());
            float y = AltitudeLayer.Terrain.AltitudeFor();
            if (subMesh != null && AllowRenderingFor(cellTerrain.def))
            {
                int count = subMesh.verts.Count;
                subMesh.verts.Add(new Vector3((float)intVec.x, y, (float)intVec.z));
                subMesh.verts.Add(new Vector3((float)intVec.x, y, (float)(intVec.z + 1)));
                subMesh.verts.Add(new Vector3((float)(intVec.x + 1), y, (float)(intVec.z + 1)));
                subMesh.verts.Add(new Vector3((float)(intVec.x + 1), y, (float)intVec.z));
                subMesh.colors.Add(Color.white);
                subMesh.colors.Add(Color.white);
                subMesh.colors.Add(Color.white);
                subMesh.colors.Add(Color.white);
                subMesh.tris.Add(count);
                subMesh.tris.Add(count + 1);
                subMesh.tris.Add(count + 2);
                subMesh.tris.Add(count);
                subMesh.tris.Add(count + 2);
                subMesh.tris.Add(count + 3);
            }
        }

        private static readonly Dictionary<(TerrainDef, bool, ColorDef, Texture2D), Material> terrainMatCache = new Dictionary<(TerrainDef, bool, ColorDef, Texture2D), Material>();

        private static readonly Type SectionLayer_Watergen = GenTypes.GetTypeInAnyAssembly("Verse.SectionLayer_Watergen", "Verse");
    }

    [HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
    public static class Patch_GenConstruct_CanPlaceBlueprintAt
    {
        public static bool Prepare()
        {
            return NanameFloors.settings.allowPlaceFloor;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldloc_S && ((LocalBuilder)c.operand).LocalIndex == 18);
            var label = codes[pos + 2].operand;

            codes.InsertRange(pos, new List<CodeInstruction>
            {
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Isinst, typeof(TerrainDef)),
                new CodeInstruction(OpCodes.Brtrue_S, label)
            });

            return codes;
        }
    }
}
