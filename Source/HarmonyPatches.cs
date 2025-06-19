using HarmonyLib;
using RimWorld;
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
            var g_AllDefs = AccessTools.PropertyGetter(typeof(DefDatabase<TerrainDef>), "AllDefs");
            var pos = codes.FindIndex(c => c.Calls(g_AllDefs));
            codes.Insert(pos + 1, CodeInstruction.Call(typeof(TerrainGrid_ExposeTerrainGrid_Patch), "ConcatDefs"));
            return codes;
        }

        public static IEnumerable<TerrainDef> ConcatDefs(IEnumerable<TerrainDef> terrainDefs)
        {
            return terrainDefs.Concat(DefDatabase<BlendedTerrainDef>.AllDefs);
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
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(SectionLayer), "Map")),
                CodeInstruction.LoadLocal(0),
                CodeInstruction.LoadLocal(2),
                CodeInstruction.LoadLocal(3),
                CodeInstruction.LoadLocal(5),
                CodeInstruction.LoadLocal(6),
                CodeInstruction.LoadLocal(7),
                CodeInstruction.Call(typeof(Patch_SectionLayer_Terrain_Regenerate), nameof(GenerateExtraMesh))
            });
            return codes;
        }

        public static void GenerateExtraMesh(SectionLayer instance, Map map, TerrainGrid terrainGrid, CellTerrain cellTerrain, IntVec3 intVec, CellTerrain[] array, HashSet<CellTerrain> hashSet, bool[] array2)
        {
            bool AllowRenderingFor(TerrainDef terrain)
            {
                return DebugViewSettings.drawTerrainWater || !terrain.HasTag("Water");
            }

            if (cellTerrain.def is BlendedTerrainDef blendedTerrainDef)
            {
                Material GetMaterialFor(CellTerrain cellTerr)
                {
                    bool polluted = cellTerr.polluted && cellTerr.snowCoverage < 0.4f && cellTerr.def.graphicPolluted != BaseContent.BadGraphic;
                    var color = cellTerr.color;
                    var key = (blendedTerrainDef, polluted, color);
                    if (!terrainMatCache.ContainsKey(key))
                    {
                        Graphic graphic = polluted ? blendedTerrainDef.CoverGraphicPolluted : blendedTerrainDef.CoverGraphic;
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

                hashSet.Clear();
                LayerSubMesh subMesh = instance.GetSubMesh(GetMaterialFor(cellTerrain));
                if (subMesh != null && AllowRenderingFor(cellTerrain.def))
                {
                    int count = subMesh.verts.Count;
                    subMesh.verts.Add(new Vector3((float)intVec.x, 0f, (float)intVec.z));
                    subMesh.verts.Add(new Vector3((float)intVec.x, 0f, (float)(intVec.z + 1)));
                    subMesh.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)(intVec.z + 1)));
                    subMesh.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)intVec.z));
                    subMesh.colors.Add(ColorWhite);
                    subMesh.colors.Add(ColorWhite);
                    subMesh.colors.Add(ColorWhite);
                    subMesh.colors.Add(ColorWhite);
                    subMesh.tris.Add(count);
                    subMesh.tris.Add(count + 1);
                    subMesh.tris.Add(count + 2);
                    subMesh.tris.Add(count);
                    subMesh.tris.Add(count + 2);
                    subMesh.tris.Add(count + 3);
                }
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 c = intVec + GenAdj.AdjacentCellsAroundBottom[i];
                    if (!c.InBounds(map))
                    {
                        array[i] = cellTerrain;
                        continue;
                    }
                    CellTerrain cellTerrain2 = new CellTerrain(terrainGrid.TerrainAt(c), c.IsPolluted(map), map.snowGrid.GetDepth(c), terrainGrid.ColorAt(c));
                    Thing edifice = c.GetEdifice(map);
                    if (edifice != null && edifice.def.coversFloor)
                    {
                        cellTerrain2.def = TerrainDefOf.Underwall;
                    }
                    array[i] = cellTerrain2;
                    if (!cellTerrain2.Equals(cellTerrain) && cellTerrain2.def.edgeType != TerrainDef.TerrainEdgeType.Hard && cellTerrain2.def.renderPrecedence >= cellTerrain.def.renderPrecedence && !hashSet.Contains(cellTerrain2))
                    {
                        hashSet.Add(cellTerrain2);
                    }
                }
                foreach (CellTerrain intVec2 in hashSet)
                {
                    LayerSubMesh subMesh2 = instance.GetSubMesh(GetMaterialFor(intVec2));
                    if (subMesh2 == null || !AllowRenderingFor(intVec2.def))
                    {
                        continue;
                    }
                    int count = subMesh2.verts.Count;
                    subMesh2.verts.Add(new Vector3((float)intVec.x + 0.5f, 0f, (float)intVec.z));
                    subMesh2.verts.Add(new Vector3((float)intVec.x, 0f, (float)intVec.z));
                    subMesh2.verts.Add(new Vector3((float)intVec.x, 0f, (float)intVec.z + 0.5f));
                    subMesh2.verts.Add(new Vector3((float)intVec.x, 0f, (float)(intVec.z + 1)));
                    subMesh2.verts.Add(new Vector3((float)intVec.x + 0.5f, 0f, (float)(intVec.z + 1)));
                    subMesh2.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)(intVec.z + 1)));
                    subMesh2.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)intVec.z + 0.5f));
                    subMesh2.verts.Add(new Vector3((float)(intVec.x + 1), 0f, (float)intVec.z));
                    subMesh2.verts.Add(new Vector3((float)intVec.x + 0.5f, 0f, (float)intVec.z + 0.5f));
                    for (int j = 0; j < 8; j++)
                    {
                        array2[j] = false;
                    }
                    for (int k = 0; k < 8; k++)
                    {
                        if (k % 2 == 0)
                        {
                            if (array[k].Equals(intVec2))
                            {
                                array2[(k - 1 + 8) % 8] = true;
                                array2[k] = true;
                                array2[(k + 1) % 8] = true;
                            }
                        }
                        else if (array[k].Equals(intVec2))
                        {
                            array2[k] = true;
                        }
                    }
                    for (int l = 0; l < 8; l++)
                    {
                        if (array2[l])
                        {
                            subMesh2.colors.Add(ColorWhite);
                        }
                        else
                        {
                            subMesh2.colors.Add(ColorClear);
                        }
                    }
                    subMesh2.colors.Add(ColorClear);
                    for (int m = 0; m < 8; m++)
                    {
                        subMesh2.tris.Add(count + m);
                        subMesh2.tris.Add(count + (m + 1) % 8);
                        subMesh2.tris.Add(count + 8);
                    }
                }
            }
        }

        private static readonly Color32 ColorClear = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 0);

        private static readonly Color32 ColorWhite = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        private static readonly Dictionary<(BlendedTerrainDef, bool, ColorDef), Material> terrainMatCache = new Dictionary<(BlendedTerrainDef, bool, ColorDef), Material>();
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
