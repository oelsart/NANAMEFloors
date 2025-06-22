using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NanameFloors
{
    public class BlendedTerrains : GameComponent
    {
        public BlendedTerrains(Game game)
        {
        }

        public override void ExposeData()
        {
            HashSet<TerrainMask> terrainMaskList = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                terrainMaskList = new HashSet<TerrainMask>();
                terrainMaskList.AddRange(DefDatabase<BlendedTerrainDef>.AllDefs
                    .Where(d => Find.Maps.Any(m => m.terrainGrid.topGrid.Any(t => t == d)))
                    .Select(d => d.GetModExtension<TerrainMask>()));
                terrainMaskList.AddRange(Find.Maps.SelectMany(m => m.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).Concat(m.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame)
                    .Where(t => t.def.entityDefToBuild is BlendedTerrainDef)))
                    .Select(t => t.def.entityDefToBuild.GetModExtension<TerrainMask>()));
            }
            Scribe_Collections.Look(ref terrainMaskList, "terrainMaskList", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                foreach (var tMask in terrainMaskList)
                {
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
