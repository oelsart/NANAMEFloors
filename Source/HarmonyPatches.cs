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
        }
    }

    [HarmonyPatch(typeof(ShaderUtility), "SupportsMaskTex")]
    public static class ShaderUtility_SupportsMaskTex_Patch
    {
        public static void Postfix(Shader shader, ref bool __result)
        {
            __result = __result || shader == AddedShaders.TerrainHardBlend || shader == AddedShaders.TerrainHardPollutedBlend || AddedShaders.TerrainFadeRoughLinearAddBlend;
        }
    }

    [HarmonyPatch(typeof(Designator_Build), "DesignateSingleCell")]
    public static class Designator_Build_DesignateSingleCell_Patch
    {
        public static void Prefix(IntVec3 c, ref BuildableDef ___entDef, Designator_Build __instance, ref BuildableDef __state)
        {
            __state = ___entDef;
            TerrainMaskDef terrainMaskDef;
            if ((terrainMaskDef = ___entDef as TerrainMaskDef) != null)
            {
                var baseTerr = c.GetTerrain(__instance.Map);
                if (baseTerr is BlendedTerrainDef) baseTerr = baseTerr.GetModExtension<TerrainMask>().baseTerrain;
                var terrainMask = terrainMaskDef.GetModExtension<TerrainMask>();
                var terrainMask2 = new TerrainMask(terrainMask.maskTextureName, baseTerr, terrainMask.coverTerrain);
                var defName = $"{baseTerr.defName}_{terrainMask.maskTextureName}_{terrainMask.coverTerrain.defName}";
                if (DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(defName) == null)
                {
                    BlendedTerrainUtil.MakeBlendedTerrain(terrainMask2);
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

    [HarmonyPatch(typeof(Designator_Place), "DoExtraGuiControls")]
    public static class Designator_Place_DoExtraGuiControls_Patch
    {
        private static readonly IEnumerable<Texture2D> terrainMasks = TerrainMask.cachedTerrainMasks.Where(m => !NanameFloors.settings.exceptMaskList.Contains(m.name));

        private static readonly float Margin = 10f;

        private static readonly float ButtonSize = (200f - Margin * 2) / 4;

        private static readonly float WindowHeight = ButtonSize * Mathf.Ceil(terrainMasks.Count() / 4f) + Margin * 2;

        public static void Postfix(float leftX, float bottomY, Designator_Place __instance)
        {
            BuildableDef def = __instance.PlacingDef;
            if (!(def is TerrainDef)) return;
            Find.WindowStack.ImmediateWindow(73095, new Rect(leftX, bottomY - WindowHeight, 200f, WindowHeight), WindowLayer.GameUI, delegate
            {
                foreach (var (terrainMaskTex, index) in terrainMasks.Select((t, i) => (t, i)))
                {
                    bool isSelected = def.GetModExtension<TerrainMask>()?.maskTextureName == terrainMaskTex.name;
                    Rect rect = new Rect(Margin + ButtonSize * (index % 4), Margin + ButtonSize * (index / 4), ButtonSize, ButtonSize);
                    Widgets.DrawTextureFitted(rect.ContractedBy(5f), terrainMasks.ElementAt(index), 1f);
                    Widgets.DrawBox(rect.ContractedBy(5f));
                    Widgets.DrawHighlightIfMouseover(rect);
                    if (Widgets.ButtonInvisible(rect))
                    {
                        if (!isSelected)
                        {
                            TerrainDef coverTerrain = def is TerrainMaskDef ? def.GetModExtension<TerrainMask>().coverTerrain : (TerrainDef)def;
                            TerrainMaskDef terrainMask = DefDatabase<TerrainMaskDef>.GetNamedSilentFail($"{coverTerrain.defName}_{terrainMaskTex.name}");
                            if (terrainMask == null)
                            {
                                terrainMask = new TerrainMaskDef();
                                foreach (var field in typeof(TerrainDef).GetFields())
                                {
                                    field.SetValue(terrainMask, field.GetValue(coverTerrain));
                                }
                                terrainMask.defName = $"{coverTerrain.defName}_{terrainMaskTex.name}";
                                terrainMask.label = $"{coverTerrain.label} {terrainMaskTex.name.Replace("_", " ")}";
                                terrainMask.costList = coverTerrain.CostList.Select(c => new ThingDefCountClass(c.thingDef, Mathf.CeilToInt(c.count / 2f))).ToList();
                                terrainMask.modExtensions = new List<DefModExtension>() { new TerrainMask(terrainMaskTex.name, coverTerrain) };
                                DefGenerator.AddImpliedDef(terrainMask);
                            }
                            AccessTools.Field(typeof(Designator_Build), "entDef").SetValue(__instance, terrainMask);
                        }
                        else AccessTools.Field(typeof(Designator_Build), "entDef").SetValue(__instance, def.GetModExtension<TerrainMask>().coverTerrain);
                        Event.current.Use();
                    }
                    if (isSelected)
                    {
                        Widgets.DrawHighlightSelected(rect);
                    }
                }
            });
        }
    }

    [HarmonyPatch(typeof(MaterialPool), "MatFrom", new Type[] { typeof(MaterialRequest) })]
    public static class MaterialPool_MatFrom_Patch
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
                CodeInstruction.Call(typeof(MaterialPool_MatFrom_Patch), "ForceCreateMaterial"),
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
            if (!req.shaderParameters.NullOrEmpty<ShaderParameter>())
            {
                for (int i = 0; i < req.shaderParameters.Count; i++)
                {
                    req.shaderParameters[i].Apply(material);
                }
            }
            return material;
        }
    }

    [HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt_NewTemp")]
    public static class GenConstruct_CanPlaceBlueprintAt_NewTemp_Patch
    {
        public static void Postfix(BuildableDef entDef, IntVec3 center, Map map, ref AcceptanceReport __result)
        {
            if (entDef is TerrainMaskDef)
            {
                var terrainMask = entDef.GetModExtension<TerrainMask>();
                var baseTerrain = center.GetTerrain(map);
                var coverTerrain = terrainMask.coverTerrain;
                if (baseTerrain == coverTerrain || baseTerrain == coverTerrain.burnedDef)
                {
                    __result = new AcceptanceReport("TerrainIsAlready".Translate(baseTerrain.label));
                }
                else if (baseTerrain is BlendedTerrainDef)
                {
                    var terrainMask2 = baseTerrain.GetModExtension<TerrainMask>();
                    if (terrainMask.maskTextureName == terrainMask2.maskTextureName && coverTerrain == terrainMask2.coverTerrain)
                    {
                        __result = new AcceptanceReport("TerrainIsAlready".Translate(coverTerrain.label));
                    }
                }
            }
        }
    }
}
