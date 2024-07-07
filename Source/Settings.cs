using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class Settings : ModSettings
    {
        public List<string> exceptMaskList = new List<string>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref exceptMaskList, "exceptMaskList");
            base.ExposeData();
        }
    }
}
