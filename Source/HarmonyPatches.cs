using System;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace NanameFloors;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
  static HarmonyPatches()
  {
    var harmony = new Harmony("com.harmony.rimworld.nanamefloors");
    var assembly = Assembly.GetExecutingAssembly();
    harmony.PatchAllUncategorized(assembly);
    
    if (AsAboveSoBelow.Active)
	    harmony.PatchCategory(assembly, AsAboveSoBelow.PatchCategory);
  }
}

[HarmonyPatch(typeof(Designator_Build), "DesignateSingleCell")]
[HarmonyBefore("Uuugggg.rimworld.Replace_Stuff.main")]
public static class Patch_Designator_Build_DesignateSingleCell
{
  public static void Prefix(IntVec3 c, ref BuildableDef ___entDef, Designator_Build __instance,
    ref BuildableDef __state)
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
      Find.WindowStack.ImmediateWindow(9359779, NanameFloors.UI.windowRect, WindowLayer.GameUI,
        () => NanameFloors.UI.DoWindowContents());
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
	    if (__instance.DrawStyleCategory is { styles.Count: > 1 })
		    bottomY -= 90f;
      GUIDoRotationControls(leftX, bottomY, NanameFloors.UI.rotation, rot => { NanameFloors.UI.rotation = rot; });
    }
  }
  
  private static void GUIDoRotationControls(float leftX, float bottomY, Rot4 rot, Action<Rot4> rotSetter)
  {
	  var winRect = new Rect(leftX, bottomY - 90f, 200f, 90f);
	  Find.WindowStack.ImmediateWindow(73095, winRect, WindowLayer.GameUI, (Action) (() =>
	  {
		  var RotDir = RotationDirection.None;
		  Text.Anchor = TextAnchor.MiddleCenter;
		  Text.Font = GameFont.Medium;
		  var rect1 = new Rect((float) (winRect.width / 2.0 - 64.0 - 5.0), 15f, 64f, 64f);
		  if (Widgets.ButtonImage(rect1, TexUI.RotLeftTex))
		  {
			  SoundDefOf.DragSlider.PlayOneShotOnCamera();
			  RotDir = RotationDirection.Counterclockwise;
			  Event.current.Use();
		  }
		  if (!SteamDeck.IsSteamDeck && NAF_DefOf.NAF_Designator_RotateLeft.MainKey != KeyCode.None)
			  Widgets.Label(rect1, NAF_DefOf.NAF_Designator_RotateLeft.MainKeyLabel);
		  var rect2 = new Rect((float) (winRect.width / 2.0 + 5.0), 15f, 64f, 64f);
		  if (Widgets.ButtonImage(rect2, TexUI.RotRightTex))
		  {
			  SoundDefOf.DragSlider.PlayOneShotOnCamera();
			  RotDir = RotationDirection.Clockwise;
			  Event.current.Use();
		  }
		  if (!SteamDeck.IsSteamDeck && NAF_DefOf.NAF_Designator_RotateRight.MainKey != KeyCode.None)
			  Widgets.Label(rect2, NAF_DefOf.NAF_Designator_RotateRight.MainKeyLabel);
		  if (RotDir != RotationDirection.None)
		  {
			  rot.Rotate(RotDir);
			  rotSetter(rot);
		  }
		  Text.Anchor = TextAnchor.UpperLeft;
		  Text.Font = GameFont.Small;
	  }));
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

    if (NAF_DefOf.NAF_Designator_RotateRight.KeyDownEvent)
    {
      rotationDirection = RotationDirection.Clockwise;
    }

    if (NAF_DefOf.NAF_Designator_RotateLeft.KeyDownEvent)
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
      new CodeInstruction(OpCodes.Ldnull),
      CodeInstruction.Call(typeof(BlendedTerrainUtil), nameof(BlendedTerrainUtil.GenerateCover))
    ]);
    return codes;
  }
}

//斜め床の下のTerrainが着色されるのを防ぐためにisPaintableじゃなければGetColoredVersionをスキップするパッチ
[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.GetMaterial))]
public static class Patch_TerrainGrid_GetMaterial
{
  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
    ILGenerator generator)
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
    var localBuilder = codes.Select(c => c.operand).OfType<LocalBuilder>()
      .LastOrDefault(l => l.LocalType == typeof(Thing));
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

[HarmonyPatch(typeof(Gravship), nameof(Gravship.Terrains), MethodType.Getter)]
public static class Patch_Gravship_Terrains
{
  private static bool Prepare() => ModsConfig.OdysseyActive;

  public static void Prefix(Rot4 ___tmpTerrainRot, ref Rot4 __state)
  {
    __state = ___tmpTerrainRot;
  }

  public static void Postfix(Rot4 ___tmpTerrainRot, Rot4 __state, Dictionary<IntVec3, TerrainDef> __result)
  {
    if (___tmpTerrainRot != __state)
    {
      var relativeRotation = Rot4.GetRelativeRotation(__state, ___tmpTerrainRot);
      foreach (var key in __result.Keys.ToArray())
      {
        if (__result[key] is BlendedTerrainDef blendedTerrainDef)
        {
          __result[key] = blendedTerrainDef.Rotated(relativeRotation);
        }
      }
    }
  }
}

[HarmonyPatch]
public static class Patches_WaterFreezes
{
  private static readonly HashSet<string> FreezableWater;
  
  private static bool Prepare() => ModsConfig.IsActive("Mlie.WaterFreezes");

  static Patches_WaterFreezes()
  {
    if (Prepare())
    {
      FreezableWater = AccessTools.StaticFieldRefAccess<HashSet<string>>("WF.WaterFreezesStatCache:FreezableWater");
    }
  }

  [HarmonyPatch("WF.TerrainDefExtensions", "IsFreezableWater")]
  [HarmonyPrefix]
  public static bool TerrainDefExtensions_IsFreezableWater_Prefix(TerrainDef def, ref bool __result)
  {
    if (def is BlendedTerrainDef blendedTerrainDef)
    {
      __result = FreezableWater.Contains(blendedTerrainDef.BaseTerrain.defName) ||
                 FreezableWater.Contains(blendedTerrainDef.CoverTerrain.defName);
      return false;
    }
    return true;
  }

  [HarmonyPatch("WF.WaterFreezesStatCache", "GetExtension")]
  [HarmonyPrefix]
  public static void WaterFreezesStatCache_GetExtension_Prefix(ref TerrainDef def)
  {
    if (def is BlendedTerrainDef blendedTerrainDef)
    {
      def = FreezableWater.Contains(blendedTerrainDef.BaseTerrain.defName)
        ? blendedTerrainDef.BaseTerrain
        : blendedTerrainDef.CoverTerrain;
    }
  }
}