// By Fulgen
// Usage example: 
/*
Composer
	.BeginChildElements(ElementBounds.Fixed(0.0, 45.0, fixedWidth, 600.0).WithAlignment(EnumDialogArea.CenterFixed))
		.AddScrollableArea(elementBounds3, (composer, bounds) =>
		{
			composer.AddCellList(bounds.WithFixedPadding(10.0), OnRequireNewCell, OnMouseDownOnCellLeft, null, new(), "mods");
		}, key: "scrollableArea")
	.EndChildElements()
*/

using System;
using System.Reflection;

using Vintagestory.API.Client;

namespace GuiExtensions
{
	public class GuiScrollableArea : GuiElement
	{
		public ElementBounds ClippingBounds { get; private set; }
		public ElementBounds ContentBounds { get; private set; }

		public GuiElementScrollbar Scrollbar { get; private set; }

		private static int scrollableId;

		public GuiScrollableArea(GuiComposer composer, ElementBounds bounds, Action<GuiElementContainer, ElementBounds> addElements, string key = null, double padding = 3.0, Action<float> onNewScrollbarValue = null, int insetDepth = 4, float insetBrightness = 0.85f) : base(composer.Api, bounds)
		{
			float scrollbarWidth = 16;

			ElementBounds areaBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, bounds.fixedWidth - scrollbarWidth, bounds.fixedHeight);
			ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightTop, 0, 0, scrollbarWidth, bounds.fixedHeight);
			bounds.WithChildren(areaBounds, scrollbarBounds);

			ClippingBounds = areaBounds.ForkContainingChild(padding, padding, padding, padding);
			ContentBounds = ClippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -padding);
			ContentBounds.fixedY = 0;

			Scrollbar = new GuiElementScrollbar(composer.Api, (value) =>
			{
				ContentBounds.fixedY = -value;
				ContentBounds.CalcWorldBounds();
				onNewScrollbarValue?.Invoke(value);
			}, scrollbarBounds);

			string containerKey = (key ?? "scrollbar" + scrollableId++) + "_container";

			composer
				.AddInset(areaBounds, insetDepth, insetBrightness)
				.AddInteractiveElement(Scrollbar)
				.BeginClip(ClippingBounds)
				.AddContainer(ContentBounds, containerKey);

			GuiElementContainer container = composer.GetContainer(containerKey);

			addElements(container, ContentBounds);

			composer.EndClip();
		}

		public void ScrollToTop()
		{
			typeof(GuiElementScrollbar).GetMethod("SetScrollbarPosition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Scrollbar, new object[] { 0 });
		}

		public void ScrollToBottom()
		{
			Scrollbar.ScrollToBottom();
		}

		public void CalcTotalHeight()
		{
			Scrollbar.SetHeights((float) ClippingBounds.fixedHeight, (float) ContentBounds.fixedHeight);
		}
	}

	public static partial class GuiComposerHelpers
	{
		public static GuiComposer AddScrollableArea(this GuiComposer composer, ElementBounds bounds, Action<GuiElementContainer, ElementBounds> addElements, string key = null, double padding = 3.0, Action<float> onNewScrollbarValue = null, int insetDepth = 4, float insetBrightness = 0.85f)
		{
			if (!composer.Composed)
			{
				composer.AddStaticElement(new GuiScrollableArea(composer, bounds, addElements, key, padding, onNewScrollbarValue, insetDepth, insetBrightness), key);
			}

			return composer;
		}

		public static GuiScrollableArea GetScrollableArea(this GuiComposer composer, string key)
		{
			return (GuiScrollableArea) composer.GetElement(key);
		}
	}
}
