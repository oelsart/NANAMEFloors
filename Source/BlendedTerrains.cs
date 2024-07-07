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
            var terrainMaskList = new HashSet<TerrainMask>();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                terrainMaskList = DefDatabase<BlendedTerrainDef>.AllDefs
                    .Where(d => Find.Maps.Any(m => m.terrainGrid.topGrid.Any(t => t == d)))
                    .Select(d => d.GetModExtension<TerrainMask>()).ToHashSet();
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
