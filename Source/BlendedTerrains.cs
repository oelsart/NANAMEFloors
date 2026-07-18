using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NanameFloors;
#pragma warning disable CS9113 // パラメーターが未読です。
public class BlendedTerrains(Game game) : GameComponent
#pragma warning restore CS9113 // パラメーターが未読です。
{
  public override void ExposeData()
  {
    HashSet<TerrainMask> terrainMaskSet = null;
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

      var allBlendedTerrains =
        Find.Maps.SelectMany(AllTerrainOnMap).Distinct().Where(t => t is BlendedTerrainDef).ToList();
      terrainMaskSet =
      [
        .. DefDatabase<BlendedTerrainDef>.AllDefs
          .Where(allBlendedTerrains.Contains)
          .Select(d => d.GetModExtension<TerrainMask>()),
        .. Find.Maps.SelectMany(m => m.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).Concat(m.listerThings
            .ThingsInGroup(ThingRequestGroup.BuildingFrame)
            .Where(t => t.def.entityDefToBuild is BlendedTerrainDef)))
          .Select(t => t.def.entityDefToBuild.GetModExtension<TerrainMask>()),
      ];
      terrainMaskSet.Remove(null);
    }

    Scribe_Collections.Look(ref terrainMaskSet, "terrainMaskList", LookMode.Deep);

    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      if (terrainMaskSet is null) return;
      foreach (var tMask in terrainMaskSet)
      {
        if (tMask?.baseTerrain is null || tMask.coverTerrain is null ||
            string.IsNullOrEmpty(tMask.maskTextureName)) continue;
        if (DefDatabase<BlendedTerrainDef>.GetNamedSilentFail(tMask.DefName) == null)
        {
          BlendedTerrainUtil.MakeBlendedTerrain(tMask);
        }
      }
    }
  }
}