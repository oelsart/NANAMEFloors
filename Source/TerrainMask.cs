using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class TerrainMask : DefModExtension, IExposable
    {
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

        public static List<Texture2D> cachedTerrainMasks = ContentFinder<Texture2D>.GetAllInFolder("NanameFloors/TerrainMasks").ToList();
    }
}
