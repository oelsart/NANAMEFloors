using System.Linq;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class NanameFloors : Mod
    {

        public NanameFloors(ModContentPack content) : base(content)
        {
            NanameFloors.settings = GetSettings<Settings>();
            NanameFloors.content = content;
            NanameFloors.UI = new UI_SelectTerrainShape();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var Rect = new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeight);
            Widgets.CheckboxLabeled(Rect, "NAF.Settings.AllowPlaceFloor".Translate(), ref settings.allowPlaceFloor);
            var Rect2 = new Rect(inRect.x, Rect.yMax, inRect.width, Text.LineHeight);
            Widgets.Label(Rect2.LeftHalf(), "NAF.Settings.ButtonSize".Translate());
            this.buttonSize = (int)NanameFloors.settings.buttonSize;
            Widgets.IntEntry(Rect2.RightHalf(), ref this.buttonSize, ref this.buff, 1);
            NanameFloors.settings.buttonSize = this.buttonSize;

            var outRect = new Rect(inRect.x, Rect2.yMax, inRect.width, Text.LineHeight);
            outRect.yMax = inRect.yMax;
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width, TerrainMask.cachedTerrainMasks.Count() * Text.LineHeight);
            Widgets.DrawMenuSection(outRect);
            Widgets.AdjustRectsForScrollView(inRect, ref outRect, ref viewRect);
            var rect = new Rect(viewRect.x, viewRect.y, viewRect.width, Text.LineHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            foreach(var terrainMask in TerrainMask.cachedTerrainMasks)
            {
                Widgets.DrawTextureFitted(new Rect(rect.x, rect.y, Text.LineHeight, Text.LineHeight), terrainMask, 0.8f);

                var labelRect = new Rect(rect.x + Text.LineHeight + 10f, rect.y, rect.width - Text.LineHeight * 2 - 20f, rect.height);
                Widgets.Label(labelRect, terrainMask.name.Translate().Truncate(labelRect.width));

                var checkBoxRect = new Rect(rect.xMax - Text.LineHeight, rect.y, Text.LineHeight, Text.LineHeight);
                Widgets.CheckboxDraw(checkBoxRect.x, checkBoxRect.y, !settings.exceptMaskList.Contains(terrainMask.name), false, Text.LineHeight);
                if (Widgets.ButtonInvisible(checkBoxRect))
                {
                    if (settings.exceptMaskList.Contains(terrainMask.name))
                    {
                        settings.exceptMaskList.Remove(terrainMask.name);
                    }
                    else
                    {
                        settings.exceptMaskList.Add(terrainMask.name);
                    }
                }
                rect.y += Text.LineHeight;
            }
            Widgets.EndScrollView();
            Text.Font = GameFont.Small;
        }

        public override string SettingsCategory()
        {
            return "Naname Floors";
        }

        public static ModContentPack content;

        public static Settings settings;

        public static UI_SelectTerrainShape UI;

        private int buttonSize;

        private string buff;

        private Vector2 scrollPosition = Vector2.zero;
    }
}
