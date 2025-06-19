using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection.Emit;

namespace NanameFloors
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.harmony.rimworld.nanamefloors");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (ModsConfig.IsActive("dubwise.dubsmintmenus"))
            {
                harmony.Patch(AccessTools.Method(AccessTools.TypeByName("MainTabWindow_MintArchitect"), "DoWindowContents"), null, AccessTools.Method(typeof(Patch_MainTabWindow_MintArchitect_DoWindowContents), "Postfix"));
            }
            if (NanameFloors.settings.allowPlaceFloor)
            {
                harmony.Patch(AccessTools.Method(typeof(GenConstruct), "CanPlaceBlueprintAt_NewTemp"), null, null, AccessTools.Method(typeof(Patch_GenConstruct_CanPlaceBlueprintAt_NewTemp), "Transpiler"));
            }
        }
    }

    [HarmonyPatch(typeof(ShaderUtility), "SupportsMaskTex")]
    public static class Patch_ShaderUtility_SupportsMaskTex
    {
        public static void Postfix(Shader shader, ref bool __result)
        {
            __result = __result || shader == AddedShaders.TerrainHardBlend || shader == AddedShaders.TerrainHardPollutedBlend || shader == AddedShaders.TerrainFadeRoughLinearAddBlend;
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

    [HarmonyPatch(typeof(MainTabWindow_Architect), "DoWindowContents")]
    public static class Patch_MainTabWindow_Architect_DoWindowContents
    {
        public static void Postfix(ArchitectCategoryTab ___selectedDesPanel)
        {
            var def = ___selectedDesPanel?.def;
            if (def == DesignationCategoryDefOf.Floors || TerraInMenuOpened(def))
            {
                Find.WindowStack.ImmediateWindow(9359779, NanameFloors.UI.windowRect, WindowLayer.GameUI, () => NanameFloors.UI.DoWindowContents());
            }
        }

        public static bool TerraInMenuOpened(DesignationCategoryDef def)
        {
            if (TerraIn_ArchitectMenu == null) return false;
            return def == TerraIn_ArchitectMenu;
        }

        public static DesignationCategoryDef TerraIn_ArchitectMenu = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("TerraIn_ArchitectMenu");
    }

    public static class Patch_MainTabWindow_MintArchitect_DoWindowContents
    {
        public static void Postfix(ArchitectCategoryTab ___SelectedTab)
        {
            var def = ___SelectedTab?.def;
            if (___SelectedTab?.def == DesignationCategoryDefOf.Floors || Patch_MainTabWindow_Architect_DoWindowContents.TerraInMenuOpened(def))
            {
                Find.WindowStack.ImmediateWindow(9359779, NanameFloors.UI.windowRect, WindowLayer.GameUI, () => NanameFloors.UI.DoWindowContents());
            }
        }
    }

    [HarmonyPatch(typeof(MaterialPool), "MatFrom", new Type[] { typeof(MaterialRequest) })]
    public static class Patch_MaterialPool_MatFrom
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ILGenerator)
        {
            var codes = instructions.ToList();
            var pos = codes.FirstIndexOf(c => c.opcode == OpCodes.Stfld && (c.operand as FieldInfo) == AccessTools.Field(typeof(MaterialRequest), "colorTwo"));
            var labelTrue = ILGenerator.DefineLabel();
            var labelFalse = ILGenerator.DefineLabel();
            var addedCodes = new List<CodeInstruction>
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(MaterialRequest), "shader"),
                CodeInstruction.LoadField(typeof(AddedShaders), "TerrainHardBlend"),
                CodeInstruction.Call(typeof(UnityEngine.Object), "op_Equality"),
                new CodeInstruction(OpCodes.Brtrue_S, labelTrue),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(MaterialRequest), "shader"),
                CodeInstruction.LoadField(typeof(AddedShaders), "TerrainHardPollutedBlend"),
                CodeInstruction.Call(typeof(UnityEngine.Object), "op_Equality"),
                new CodeInstruction(OpCodes.Brtrue_S, labelTrue),
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(MaterialRequest), "shader"),
                CodeInstruction.LoadField(typeof(AddedShaders), "TerrainFadeRoughLinearAddBlend"),
                CodeInstruction.Call(typeof(UnityEngine.Object), "op_Equality"),
                new CodeInstruction(OpCodes.Brfalse_S, labelFalse),
                CodeInstruction.LoadArgument(0).WithLabels(labelTrue),
                CodeInstruction.Call(typeof(Patch_MaterialPool_MatFrom), "ForceCreateMaterial"),
                new CodeInstruction(OpCodes.Ret)
            };
            codes[pos + 1] = codes[pos + 1].WithLabels(labelFalse);
            codes.InsertRange(pos + 1, addedCodes);
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
                material.SetColor(ShaderPropertyIDs.ColorTwo, req.colorTwo);
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

    public static class Patch_GenConstruct_CanPlaceBlueprintAt_NewTemp
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Ldloc_S && ((LocalBuilder)c.operand).LocalIndex == 29);
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
