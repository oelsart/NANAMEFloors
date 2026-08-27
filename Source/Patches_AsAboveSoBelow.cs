using HarmonyLib;
using Verse;

namespace NanameFloors;

[HarmonyPatchCategory(AsAboveSoBelow.PatchCategory)]
[HarmonyPatch("AsAboveSoBelow.SectionLayer_ABBelowV2", "PrintBelowTerrainEdges")]
public static class Patch_SectionLayer_ABBelowV2_PrintBelowTerrainEdges
{
	public static void Postfix(SectionLayer __instance, IntVec3 above,
		CellTerrain self, int depth, int levels)
	{
		if (self.def is not BlendedTerrainDef blendedTerrainDef) return;
		
		var material = blendedTerrainDef.CoverTerrain.dontRender
			? MatBases.ShadowMask
			: BlendedTerrainUtil.GetMaterial(self, blendedTerrainDef);
		
		BlendedTerrainUtil.GenerateCover(__instance, self, above, AsAboveSoBelow.DepthQueue(material, depth, levels));
	}
}

[HarmonyPatchCategory(AsAboveSoBelow.PatchCategory)]
[HarmonyPatch("AsAboveSoBelow.SectionLayer_ABBelowWatergen", "EmitDepthEdges")]
public static class Patch_SectionLayer_ABBelowWatergen_EmitDepthEdges
{
	public static void Postfix(SectionLayer __instance, TerrainGrid grid, IntVec3 below, IntVec3 above)
	{
		if (grid.TerrainAt(below) is not BlendedTerrainDef blendedTerrainDef) return;
		var cellTerrain = new CellTerrain(blendedTerrainDef, false, 0f, 0f, null); // Watergenの場合defしか使わない
		
		BlendedTerrainUtil.GenerateCover(__instance, cellTerrain, above, blendedTerrainDef.CoverWaterDepthMaterial);
	}
}