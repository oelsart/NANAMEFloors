using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameFloors;

public class Settings : ModSettings
{
  public List<string> exceptMaskList = [];
  public Rect windowRect = new (0f, 0f, 156f, 144f);
  public float buttonSize = 38f;
  public bool allowPlaceFloor = true;
  public bool showExtraGui = true;

  public override void ExposeData()
  {
    Scribe_Collections.Look(ref exceptMaskList, "exceptMaskList");
    var windowRectX = windowRect.x;
    var windowRectY = windowRect.y;
    var windowRectWidth = windowRect.width;
    var windowRectHeight = windowRect.height;
    Scribe_Values.Look(ref windowRectX, nameof(windowRectX));
    Scribe_Values.Look(ref windowRectY, nameof(windowRectY));
    Scribe_Values.Look(ref windowRectWidth, nameof(windowRectWidth), 156f);
    Scribe_Values.Look(ref windowRectHeight, nameof(windowRectHeight), 144f);
    Scribe_Values.Look(ref buttonSize, nameof(buttonSize), 38f);
    Scribe_Values.Look(ref allowPlaceFloor, nameof(allowPlaceFloor), true);
    Scribe_Values.Look(ref showExtraGui, nameof(showExtraGui), true);

    switch (Scribe.mode)
    {
	    case LoadSaveMode.LoadingVars:
		    windowRect = new Rect(windowRectX, windowRectY, windowRectWidth, windowRectHeight);
		    break;
	    case LoadSaveMode.Saving:
		    NanameFloors.UI.terrainMasks.Clear();
		    NanameFloors.UI.terrainMasks.AddRange(
			    TerrainMask.cachedTerrainMasks.Where(m => !exceptMaskList.Contains(m.name)));
		    break;
	    case LoadSaveMode.Inactive:
	    case LoadSaveMode.ResolvingCrossRefs:
	    case LoadSaveMode.PostLoadInit:
	    default:
		    break;
    }

    base.ExposeData();
  }
}