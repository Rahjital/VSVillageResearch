using System;
using System.Reflection;

using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

using HarmonyLib;
using VillageResearch.HarmonyPatches;

namespace GuiExtensions
{
	public class GuiElementScrollableTextInput : GuiElementTextInput
	{
		private static FieldInfo bottomSpacingField;
		private static FieldInfo rightSpacingField;
		private static FieldInfo renderLeftOffsetField;
		private static FieldInfo textTextureField;
		private static FieldInfo textSizeField;

		public GuiElementScrollableTextInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> OnTextChanged, CairoFont font) : base(capi, bounds, OnTextChanged, font)
        {
			if (bottomSpacingField is null)
			{
				bottomSpacingField = typeof(GuiElementScrollableTextInput).GetField("bottomSpacing", BindingFlags.Instance | BindingFlags.NonPublic);
				rightSpacingField = typeof(GuiElementScrollableTextInput).GetField("rightSpacing", BindingFlags.Instance | BindingFlags.NonPublic);
				renderLeftOffsetField = typeof(GuiElementScrollableTextInput).GetField("renderLeftOffset", BindingFlags.Instance | BindingFlags.NonPublic);
				textTextureField = typeof(GuiElementScrollableTextInput).GetField("textTexture", BindingFlags.Instance | BindingFlags.NonPublic);
				textSizeField = typeof(GuiElementScrollableTextInput).GetField("textSize", BindingFlags.Instance | BindingFlags.NonPublic);
			}
        }

		public override void RenderInteractiveElements(float deltaTime)
        {
			double renderLeftOffset = (double)renderLeftOffsetField.GetValue(this);

            if (HasFocus)
            {
                api.Render.GlToggleBlend(true);
                api.Render.Render2DTexture(highlightTexture.TextureId, highlightBounds);
            } 
			/*else
            {
                if (placeHolderTextTexture != null && (text == null || text.Length == 0) && (lines == null || lines.Count == 0 || lines[0] == null || lines[0] == ""))
                {
                    api.Render.GlToggleBlend(true);
                    api.Render.Render2DTexturePremultipliedAlpha(
                        placeHolderTextTexture.TextureId, 
                        (int)(highlightBounds.renderX + highlightBounds.absPaddingX + 3), 
                        (int)(highlightBounds.renderY + highlightBounds.absPaddingY + (highlightBounds.OuterHeight - placeHolderTextTexture.Height) / 2), 
                        placeHolderTextTexture.Width, 
                        placeHolderTextTexture.Height
                    );
                    
                }
            }*/

			double bottomSpacing = (double)bottomSpacingField.GetValue(this);
			double rightSpacing = (double)rightSpacingField.GetValue(this);
			LoadedTexture textTexture = (LoadedTexture)textTextureField.GetValue(this);
			Vec2i textSize = (Vec2i)textSizeField.GetValue(this);

            /*api.Render.GlScissor(
                (int)(Bounds.renderX), 
                (int)(api.Render.FrameHeight - Bounds.renderY - Bounds.InnerHeight), 
                Math.Max(0, Bounds.OuterWidthInt + 1 - (int)rightSpacing), 
                Math.Max(0, Bounds.OuterHeightInt + 1 - (int)bottomSpacing)
            );*/

            //api.Render.GlScissorFlag(true);
			api.Render.PushScissor(Bounds, true);
            api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds.renderX - renderLeftOffset, Bounds.renderY, textSize.X, textSize.Y);
            api.Render.PopScissor();
			//api.Render.GlScissorFlag(false);

            //base.RenderInteractiveElements(deltaTime);
        }

		public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args) 
		{ 
			args.Handled = true;
		}
	}

	[VSHarmonyPatch(EnumPatchType.Client)]
	public class GuiElementScrollableTextInputPatch : VSHarmonyPatchBase
	{
		private static void BaseRenderInteractiveElements(object instance, float deltaTime)
		{
			throw new NotImplementedException("Called unpatched BaseGetHeldItemInfo stub in ItemArrowPatch!");
		}

		public override void Execute(Harmony harmony, ICoreAPI api)
		{
			ReversePatcher reversePatcher = harmony.CreateReversePatcher(typeof(GuiElementEditableTextBase).GetMethod("RenderInteractiveElements"), 
				new HarmonyMethod(typeof(GuiElementScrollableTextInputPatch).GetMethod("BaseRenderInteractiveElements", BindingFlags.Static | BindingFlags.NonPublic)));
			
			//reversePatcher.Patch(HarmonyReversePatchType.Snapshot); // Crashes since 1.17, probably because new Harmony version :(
			reversePatcher.Patch(HarmonyReversePatchType.Original);

			harmony.Patch(typeof(GuiElementScrollableTextInput).GetMethod("RenderInteractiveElements"),
				prefix: GetPatchMethod("RenderInteractiveElementsPrefix") 
			);
		}

		static bool RenderInteractiveElementsPrefix(GuiElementScrollableTextInput __instance, float deltaTime)
		{
			BaseRenderInteractiveElements(__instance, deltaTime);

			return true;
		}
	}
}
