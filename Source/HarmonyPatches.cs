using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NanameFloors;

[StaticConstructorOnStartup]
internal class HarmonyPatches
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
        if (NanameFloors.UI.selectedMask is null || ___entDef is not TerrainDef terrainDef) return;
        var maskTextureName = NanameFloors.UI.selectedMask.name;
        var baseTerr = c.GetTerrain(__instance.Map);
        if (baseTerr is BlendedTerrainDef) baseTerr = baseTerr.GetModExtension<TerrainMask>().baseTerrain;
        var rotation = NanameFloors.UI.rotation;
        var defName = TerrainMask.GetDefName(maskTextureName, baseTerr, terrainDef, rotation);
        if (DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(defName) == null)
        {
            var terrainMask = new TerrainMask(maskTextureName, baseTerr, terrainDef, rotation);
            BlendedTerrainUtil.MakeBlendedTerrain(terrainMask);
        }
        ___entDef = DefDatabase<BlendedTerrainDef>.GetNamed(defName);
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
        if (__instance.PlacingDef.IsNanameSupported)
        {
            Find.WindowStack.ImmediateWindow(9359779, NanameFloors.UI.windowRect, WindowLayer.GameUI, () => NanameFloors.UI.DoWindowContents());
        }
    }
}

[HarmonyPatch(typeof(Designator_Place), nameof(Designator_Place.DoExtraGuiControls))]
public static class Patch_Designator_Place_DoExtraGuiControls
{
    public static void Postfix(Designator_Place __instance, float leftX, float bottomY)
    {
        if (__instance.PlacingDef.IsNanameSupported)
        {
            DesignatorUtility.GUIDoRotationControls(leftX, bottomY, NanameFloors.UI.rotation, rot =>
            {
                NanameFloors.UI.rotation = rot;
            });
        }
    }
}

[HarmonyPatch(typeof(Designator_Place), nameof(Designator_Place.SelectedProcessInput))]
public static class Patch_Designator_Place_SelectedProcessInput
{
    public static void Postfix(Designator_Place __instance)
    {
        if (__instance.PlacingDef.IsNanameSupported)
        {
            HandleRotationShortcuts();
        }
    }

    private static float middleMouseDownTime;
    
    private static void HandleRotationShortcuts()
    {
        var rotationDirection = RotationDirection.None;
        if (Event.current.button == 2)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                middleMouseDownTime = Time.realtimeSinceStartup;
            }
            if (Event.current.type == EventType.MouseUp && Time.realtimeSinceStartup - middleMouseDownTime < 0.15f)
            {
                rotationDirection = RotationDirection.Clockwise;
            }
        }
        if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
        {
            rotationDirection = RotationDirection.Clockwise;
        }
        if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
        {
            rotationDirection = RotationDirection.Counterclockwise;
        }
        if (rotationDirection != RotationDirection.None)
        {
            HandleRotation(rotationDirection);
        }
    }

    private static void HandleRotation(RotationDirection dir)
    {
        NanameFloors.UI.rotation.Rotate(dir);
        SoundDefOf.DragSlider.PlayOneShotOnCamera();
    }
}

[HarmonyPatch(typeof(TerrainGrid), "ExposeTerrainGrid")]
public static class TerrainGrid_ExposeTerrainGrid_Patch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchStartForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(DefDatabase<TerrainDef>),
                    nameof(DefDatabase<>.AllDefs))))
            .InsertAfter(CodeInstruction.Call(typeof(TerrainGrid_ExposeTerrainGrid_Patch), nameof(ConcatDefs)))
            .InstructionEnumeration();
    }

    public static IEnumerable<TerrainDef> ConcatDefs(IEnumerable<TerrainDef> terrainDefs)
    {
        return terrainDefs.Concat(DefDatabase<BlendedTerrainDef>.AllDefs);
    }
}

[HarmonyPatch(typeof(SectionLayer_Terrain), nameof(SectionLayer_Terrain.Regenerate))]
public static class Patch_SectionLayer_Terrain_Regenerate
{
    private static readonly Dictionary<(TerrainDef, bool, ColorDef, Texture2D), Material> terrainMatCache = [];
    private static readonly Type SectionLayer_Watergen = GenTypes.GetTypeInAnyAssembly("Verse.SectionLayer_Watergen", "Verse");
        
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var m_MoveNext = AccessTools.Method(typeof(CellRect.Enumerator), nameof(CellRect.Enumerator.MoveNext));
        var pos = codes.FindIndex(c => c.Calls(m_MoveNext));
        codes.InsertRange(pos,
        [
            CodeInstruction.LoadArgument(0),
            CodeInstruction.LoadLocal(8),
            CodeInstruction.LoadLocal(6),
            CodeInstruction.Call(typeof(Patch_SectionLayer_Terrain_Regenerate), nameof(GenerateCover))
        ]);
        return codes;
    }

    public static void GenerateCover(SectionLayer instance, CellTerrain cellTerrain, IntVec3 intVec)
    {
        if (cellTerrain.def is not BlendedTerrainDef blendedTerrainDef) return;

        var subMesh = instance.GetSubMesh(blendedTerrainDef.CoverTerrain.dontRender ? MatBases.ShadowMask : GetMaterial());
        var y = AltitudeLayer.Terrain.AltitudeFor();
        if (subMesh != null && AllowRenderingFor(cellTerrain.def))
        {
            var count = subMesh.verts.Count;
            var color = new Color(blendedTerrainDef.Rotation.AsInt / 255f, 1f, 1f, 1f);
            subMesh.verts.Add(new Vector3(intVec.x, y, intVec.z));
            subMesh.verts.Add(new Vector3(intVec.x, y, intVec.z + 1));
            subMesh.verts.Add(new Vector3(intVec.x + 1, y, intVec.z + 1));
            subMesh.verts.Add(new Vector3(intVec.x + 1, y, intVec.z));
            subMesh.colors.Add(color);
            subMesh.colors.Add(color);
            subMesh.colors.Add(color);
            subMesh.colors.Add(color);
            subMesh.tris.Add(count);
            subMesh.tris.Add(count + 1);
            subMesh.tris.Add(count + 2);
            subMesh.tris.Add(count);
            subMesh.tris.Add(count + 2);
            subMesh.tris.Add(count + 3);
        }

        return;

        bool AllowRenderingFor(TerrainDef terrain)
        {
            return DebugViewSettings.drawTerrainWater || !terrain.HasTag("Water");
        }

        Material GetMaterial()
        {
            if (SectionLayer_Watergen.IsAssignableFrom(instance.GetType()))
            {
                return blendedTerrainDef.CoverWaterDepthMaterial;
            }

            var coverTerrain = blendedTerrainDef.CoverTerrain;
            var polluted = cellTerrain is { polluted: true, snowCoverage: < 0.4f, sandCoverage: < 0.4f } &&
                           blendedTerrainDef.CoverGraphicPolluted != BaseContent.BadGraphic;
            var color = cellTerrain.color;
            var key = (coverTerrain, polluted, color, blendedTerrainDef.MaskTex);
            if (terrainMatCache.TryGetValue(key, out var material)) return material;
            var graphic = polluted ? blendedTerrainDef.CoverGraphicPolluted ?? blendedTerrainDef.CoverGraphic : blendedTerrainDef.CoverGraphic;
            if (color != null)
            {
                terrainMatCache[key] = graphic.GetColoredVersion(graphic.Shader, color.color, Color.white).MatSingle;
                terrainMatCache[key].SetTexture(ShaderPropertyIDs.MaskTex, blendedTerrainDef.MaskTex);
            }
            else
            {
                terrainMatCache[key] = graphic.MatSingle;
            }

            return terrainMatCache[key];
        }
    }
}

//斜め床の下のTerrainが着色されるのを防ぐためにisPaintableじゃなければGetColoredVersionをスキップするパッチ
[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.GetMaterial))]
public static class Patch_TerrainGrid_GetMaterial
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var codes = instructions.ToList();
        var m_GetColoredVersion = AccessTools.Method(typeof(Graphic), nameof(Graphic.GetColoredVersion));
        var pos = codes.FindIndex(c => c.Calls(m_GetColoredVersion));
        var label = generator.DefineLabel();
        var label2 = generator.DefineLabel();

        codes[pos].labels.Add(label);
        codes[pos + 1].labels.Add(label2);
        codes.InsertRange(pos,
        [
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TerrainDef), nameof(TerrainDef.isPaintable))),
            new CodeInstruction(OpCodes.Brtrue_S, label),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Br_S, label2)
        ]);
        return codes;
    }
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
        var localBuilder = codes.Select(c => c.operand).OfType<LocalBuilder>().LastOrDefault(l => l.LocalType == typeof(Thing));
        if (localBuilder is null) return codes;
        var pos = codes.FindIndex(c => c.IsLdloc(localBuilder));
        var label = codes[pos + 2].operand;

        codes.InsertRange(pos,
        [
            CodeInstruction.LoadArgument(0),
            new CodeInstruction(OpCodes.Isinst, typeof(TerrainDef)),
            new CodeInstruction(OpCodes.Brtrue_S, label)
        ]);

        return codes;
    }
}