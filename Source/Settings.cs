using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class Settings : ModSettings
    {
        public List<string> exceptMaskList = new List<string>();
        public Rect windowRect = new Rect(0f, 0f, 156f, 144f);
        public float buttonSize = 38f;
        public bool allowPlaceFloor = true;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref exceptMaskList, "exceptMaskList");
            var windowRectX = windowRect.x;
            var windowRectY = windowRect.y;
            var windowRectWidth = windowRect.width;
            var windowRectHeight = windowRect.height;
            Scribe_Values.Look(ref windowRectX, "windowRectX", 0f);
            Scribe_Values.Look(ref windowRectY, "windowRectY", 0f);
            Scribe_Values.Look(ref windowRectWidth, "windowRectWidth", 156f);
            Scribe_Values.Look(ref windowRectHeight, "windowRectHeight", 144f);
            Scribe_Values.Look(ref buttonSize, "buttonSize", 38f);
            Scribe_Values.Look(ref allowPlaceFloor, "allowPlaceFloor", true);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                windowRect = new Rect(windowRectX, windowRectY, windowRectWidth, windowRectHeight);
            }
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                NanameFloors.UI.terrainMasks = TerrainMask.cachedTerrainMasks.Where(m => !this.exceptMaskList.Contains(m.name));
            }
            base.ExposeData();
        }
    }
}
