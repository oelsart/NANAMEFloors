using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    [StaticConstructorOnStartup]
    public class TerrainMask : DefModExtension, IExposable
    {
        static TerrainMask()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                cachedTerrainMasks = ContentFinder<Texture2D>.GetAllInFolder("NanameFloors/TerrainMasks").ToList();
                NanameFloors.UI.terrainMasks = cachedTerrainMasks.Where(m => !NanameFloors.settings.exceptMaskList.Contains(m.name)).ToList();
            });
        }

        public TerrainMask() { }

        public TerrainMask(string name, TerrainDef baseTerr, TerrainDef coverTerr)
        {
            this.maskTextureName = name;
            this.baseTerrain = baseTerr;
            this.coverTerrain = coverTerr;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref maskTextureName, "maskTextureName");
            Scribe_Defs.Look(ref baseTerrain, "baseTerrain");
            Scribe_Defs.Look(ref coverTerrain, "coverTerrain");
        }

        public string maskTextureName;

        public TerrainDef baseTerrain;

        public TerrainDef coverTerrain;

        //ここは本当はListに変更したけど互換性のためにIEnumerableのままにしとく
        //1.6からはListにするからね
        public static IEnumerable<Texture2D> cachedTerrainMasks;
    }
}
