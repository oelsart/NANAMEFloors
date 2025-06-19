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
            windowRect = NanameFloors.settings.windowRect;
            resizer = new WindowResizer();
            resizer.minWindowSize = new Vector2(ButtonSize + Margin * 2f, ButtonSize + Margin * 2f + Text.LineHeightOf(GameFont.Small) + 2f);
        }

        public void DoWindowContents()
        {
            var selectedDesignator = Find.DesignatorManager.SelectedDesignator;
            if (selectedDesignator != designator)
            {
                designator = selectedDesignator;
                selectedMask = null;
            }

            var inRect = windowRect.AtZero().ContractedBy(Margin);
            if (terrainMasks.Count() == 0) return;
            Rect labelRect;
            using (new TextBlock(GameFont.Small))
            {
                labelRect = new Rect(inRect.x, inRect.y, windowRect.width, Text.LineHeight);
                Widgets.Label(labelRect, "NAF.FloorShapes".Translate());
                Widgets.DrawLineHorizontal(labelRect.x, labelRect.yMax, labelRect.width);
            }

            GUI.DragWindow(labelRect);
            if (Mouse.IsOver(labelRect))
            {
                if (Input.GetMouseButton(0))
                {
                    Window window = Find.WindowStack.Windows.FirstOrDefault(w => w.ID == -9359779);
                    windowRect = window.windowRect;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    NanameFloors.settings.windowRect = windowRect;
                    NanameFloors.settings.Write();
                }
            }
            windowRect = resizer.DoResizeControl(windowRect);

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
                bool isSelected = terrainMaskTex == selectedMask;
                Rect rect = new Rect(viewRect.x + ButtonSize * (index % columnCount), viewRect.y + ButtonSize * (index / columnCount), ButtonSize, ButtonSize);
                Rect rect2 = rect.ContractedBy(5f);
                Widgets.DrawTextureFitted(rect2, terrainMaskTex, 1f);
                Widgets.DrawBox(rect2);
                Widgets.DrawHighlightIfMouseover(rect);
                if (Widgets.ButtonInvisible(rect))
                {
                    selectedMask = isSelected ? null : terrainMaskTex;
                    Event.current.Use();
                }
                if (isSelected)
                {
                    Widgets.DrawStrongHighlight(rect, Color.yellow * 0.8f);
                }
            }
            Widgets.EndScrollView();
            Text.Font = GameFont.Small;
        }

        public List<Texture2D> terrainMasks;

        public Texture2D selectedMask;

        public Window parent;

        public Rect windowRect;

        private Vector2 scrollPosition = Vector2.zero;

        private WindowResizer resizer;

        private Designator designator;
    }
}
