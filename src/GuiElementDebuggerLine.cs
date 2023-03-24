using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VillageResearch
{
	public enum EnumButtonStyle
	{
		None,
		MainMenu,
		Normal,
		Small
	}

	public class GuiElementDebuggerLine : GuiElementControl
	{
		LoadedTexture hoverTexture;
		
		ActionConsumable<GuiElementDebuggerLine> onClick;

		bool isOver;

		bool active = false;		
		public Vec4f MouseOverColor = new Vec4f(0, 0, 0, 0.3f);
		public Vec4f HighlightColor = null;

		public static double Padding = 2;

		public bool Visible = true;

		public override bool Focusable { get { return true; } }

		/// <summary>
		/// Creates a button with text.
		/// </summary>
		/// <param name="capi">The Client API</param>
		/// <param name="text">The text of the button.</param>
		/// <param name="font">The font of the text.</param>
		/// <param name="hoverFont">The font of the text when the player is hovering over the button.</param>
		/// <param name="onClick">The event fired when the button is clicked.</param>
		/// <param name="bounds">The bounds of the button.</param>
		/// <param name="style">The style of the button.</param>
		public GuiElementDebuggerLine(ICoreClientAPI capi, ActionConsumable<GuiElementDebuggerLine> onClick, ElementBounds bounds) : base(capi, bounds)
		{
			hoverTexture = new LoadedTexture(capi);

			this.onClick = onClick;
		}

		public override void BeforeCalcBounds()
		{
			/*normalText.AutoBoxSize(true);
			Bounds.fixedWidth = normalText.Bounds.fixedWidth;
			Bounds.fixedHeight = normalText.Bounds.fixedHeight;

			pressedText.Bounds = normalText.Bounds.CopyOnlySize();*/
		}

		public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
		{
			Bounds.CalcWorldBounds();
			//normalText.Bounds.CalcWorldBounds();

			var surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
			var ctx = genContext(surface);
			ctx.Clear();


			// 2. Hover button
			//ctx.SetSourceRGBA(0, 0, 0, 0.4);
			ctx.SetSourceRGBA(1, 1, 1, 1.0);
			ctx.Rectangle(0, 0, Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.Fill();

			generateTexture(surface, ref hoverTexture);

			ctx.Dispose();
			surface.Dispose();
		}

		public override void RenderInteractiveElements(float deltaTime)
		{
			if (!Visible) return;

			if (isOver || (HighlightColor != null))
			{
				api.Render.Render2DTexture(hoverTexture.TextureId, Bounds, color: HighlightColor ?? MouseOverColor);
			}
		}

		public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
		{
			if (!Visible) return;

			isOver = (enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY)) || active;
		}

		public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
		{
			if (!Visible) return;
			if (!enabled) return;

			base.OnMouseDownOnElement(api, args);
		}

		public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
		{
			if (!Visible) return;

			base.OnMouseUp(api, args);
		}

		public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
		{
			if (enabled && Bounds.PointInside(args.X, args.Y) && args.Button == EnumMouseButton.Left)
			{
				args.Handled = onClick(this);
			}
		}

		/// <summary>
		/// Sets the button as active or inactive.
		/// </summary>
		/// <param name="active">Active == clickable</param>
		public void SetActive(bool active)
		{
			this.active = active;
		}

		public override void Dispose()
		{
			base.Dispose();

			hoverTexture.Dispose();
		}
	}

	public static partial class GuiComposerHelpers
	{
		/// <summary>
		/// Creates a button for the current GUI.
		/// </summary>
		/// <param name="text">The text on the button.</param>
		/// <param name="onClick">The event fired when the button is clicked.</param>
		/// <param name="bounds">The bounds of the button.</param>
		/// <param name="style">The style of the button. (Default: Normal)</param>
		/// <param name="orientation">The orientation of the text. (Default: center)</param>
		/// <param name="key">The internal name of the button.</param>
		public static GuiComposer AddDebuggerLine(this GuiComposer composer, ActionConsumable<GuiElementDebuggerLine> onClick, ElementBounds bounds, string key = null)
		{
			if (!composer.Composed)
			{
				GuiElementDebuggerLine elem = new GuiElementDebuggerLine(composer.Api, onClick, bounds);
				composer.AddInteractiveElement(elem, key);
			}
			return composer;
		}

		/// <summary>
		/// Gets the button by name.
		/// </summary>
		/// <param name="key">The name of the button.</param>
		public static GuiElementDebuggerLine GetDebuggerLine(this GuiComposer composer, string key)
		{
			return (GuiElementDebuggerLine)composer.GetElement(key);
		}
	}
}