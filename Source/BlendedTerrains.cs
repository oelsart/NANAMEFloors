using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NanameFloors
{
#pragma warning disable CS9113 // パラメーターが未読です。
    public class BlendedTerrains(Game game) : GameComponent
#pragma warning restore CS9113 // パラメーターが未読です。
    {
        public override void ExposeData()
        {
            HashSet<TerrainMask> terrainMaskList = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                static IEnumerable<TerrainDef> AllTerrainOnMap(Map map)
                {
                    var terrainGrid = map.terrainGrid;
                    foreach (var c in map.AllCells)
                    {
                        var ind = map.cellIndices.CellToIndex(c);
                        yield return terrainGrid.TopTerrainAt(ind);
                        yield return terrainGrid.FoundationAt(ind);
                        yield return terrainGrid.UnderTerrainAt(ind);
                        yield return terrainGrid.TempTerrainAt(ind);
                    }
                }

                var allBlendedTerrains = Find.Maps.SelectMany(AllTerrainOnMap).Distinct().Where(t => t is BlendedTerrainDef).ToList();
                terrainMaskList =
                [
                    .. DefDatabase<BlendedTerrainDef>.AllDefs
                        .Where(allBlendedTerrains.Contains)
                        .Select(d => d.GetModExtension<TerrainMask>()),
                    .. Find.Maps.SelectMany(m => m.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).Concat(m.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame)
                        .Where(t => t.def.entityDefToBuild is BlendedTerrainDef)))
                        .Select(t => t.def.entityDefToBuild.GetModExtension<TerrainMask>()),
                ];
                terrainMaskList.RemoveWhere(t => t is null);
            }
            Scribe_Collections.Look(ref terrainMaskList, "terrainMaskList", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (terrainMaskList is null) return;
                foreach (var tMask in terrainMaskList)
                {
                    if (tMask is null || tMask.baseTerrain is null || tMask.coverTerrain is null || string.IsNullOrEmpty(tMask.maskTextureName)) continue;
                    var defName = $"{tMask.baseTerrain.defName}_{tMask.maskTextureName}_{tMask.coverTerrain.defName}";
                    if (DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(defName) == null)
                    {
                        BlendedTerrainUtil.MakeBlendedTerrain(tMask);
                    }
                }
            }
        }
    }
}
