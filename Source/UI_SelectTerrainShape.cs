using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NanameFloors
{
    public class UI_SelectTerrainShape
    {
        public float ButtonSize => NanameFloors.settings.buttonSize;

        protected float Margin => 2f;

        public UI_SelectTerrainShape()
        {
            this.windowRect = NanameFloors.settings.windowRect;
            this.resizer = new WindowResizer();
            this.resizer.minWindowSize = new Vector2(ButtonSize + this.Margin * 2f, ButtonSize + this.Margin * 2f + Text.LineHeightOf(GameFont.Small) + 2f);
        }

        public void DoWindowContents()
        {
            var inRect = this.windowRect.AtZero().ContractedBy(this.Margin);
            if (terrainMasks.Count() == 0) return;
            Rect labelRect;
            using (new TextBlock(GameFont.Small))
            {
                labelRect = new Rect(inRect.x, inRect.y, this.windowRect.width, Text.LineHeight);
                Widgets.Label(labelRect, "NAF.FloorShapes".Translate());
                Widgets.DrawLineHorizontal(labelRect.x, labelRect.yMax, labelRect.width);
            }

            GUI.DragWindow(labelRect);
            if (Mouse.IsOver(labelRect))
            {
                if (Input.GetMouseButton(0))
                {
                    Window window = Find.WindowStack.Windows.FirstOrDefault(w => w.ID == -9359779);
                    this.windowRect = window.windowRect;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    NanameFloors.settings.windowRect = this.windowRect;
                    NanameFloors.settings.Write();
                }
            }
            this.windowRect = this.resizer.DoResizeControl(this.windowRect);

            var parentRect = new Rect(inRect.x, labelRect.yMax + 2f, inRect.width, inRect.height - labelRect.height);

            var columnCount = Math.Min(terrainMasks.Count(), (int)(inRect.width / ButtonSize));
            var rowCount = Mathf.CeilToInt((float)terrainMasks.Count() / columnCount);

            var outRect = parentRect;
            var viewRect = outRect;
            viewRect.height = Math.Max(outRect.height - 1f, rowCount * ButtonSize);
            Widgets.AdjustRectsForScrollView(parentRect, ref outRect, ref viewRect);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            foreach (var (terrainMaskTex, index) in terrainMasks.Select((t, i) => (t, i)))
            {
                bool isSelected = terrainMaskTex == this.selectedMask;
                Rect rect = new Rect(viewRect.x + ButtonSize * (index % columnCount), viewRect.y + ButtonSize * (index / columnCount), ButtonSize, ButtonSize);
                Rect rect2 = rect.ContractedBy(5f);
                Widgets.DrawTextureFitted(rect2, terrainMaskTex, 1f);
                Widgets.DrawBox(rect2);
                Widgets.DrawHighlightIfMouseover(rect);
                if (Widgets.ButtonInvisible(rect))
                {
                    this.selectedMask = isSelected ? null : terrainMaskTex;
                    Event.current.Use();
                }
                if (isSelected)
                {
                    Widgets.DrawHighlightSelected(rect);
                }
            }
            Widgets.EndScrollView();
            Text.Font = GameFont.Small;
        }

        public IEnumerable<Texture2D> terrainMasks = TerrainMask.cachedTerrainMasks.Where(m => !NanameFloors.settings.exceptMaskList.Contains(m.name));

        public Texture2D selectedMask;

        public Window parent;

        public Rect windowRect;

        private Vector2 scrollPosition = Vector2.zero;

        private WindowResizer resizer;
    }
}
