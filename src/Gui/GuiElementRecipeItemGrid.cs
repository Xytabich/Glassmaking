using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace GlassMaking
{
	// Same as the GuiElementSkillItemGrid buf fixes a nasty bug with scissor bounds
	public class GuiElementRecipeItemGrid : GuiElement
	{
		private List<SkillItem> skillItems;

		private int cols;

		private int rows;

		public Action<int> OnSlotClick;

		public Action<int> OnSlotOver = default!;

		public int selectedIndex = -1;

		private LoadedTexture slotTexture;
		private LoadedTexture hoverTexture;

		public override bool Focusable => true;

		//
		// Summary:
		//     Creates a Skill Item Grid.
		//
		// Parameters:
		//   capi:
		//     The Client API
		//
		//   skillItems:
		//     The items with skills.
		//
		//   columns:
		//     The columns of the Item Grid
		//
		//   rows:
		//     The Rows of the Item Grid.
		//
		//   OnSlotClick:
		//     The event fired when the slot is clicked.
		//
		//   bounds:
		//     The bounds of the Item Grid.
		public GuiElementRecipeItemGrid(ICoreClientAPI capi, List<SkillItem> skillItems, int columns, int rows, Action<int> OnSlotClick, ElementBounds bounds)
			: base(capi, bounds)
		{
			hoverTexture = new LoadedTexture(capi);
			slotTexture = new LoadedTexture(capi);
			this.skillItems = skillItems;
			cols = columns;
			this.rows = rows;
			this.OnSlotClick = OnSlotClick;
			Bounds.fixedHeight = rows * (GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize);
			Bounds.fixedWidth = columns * (GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementPassiveItemSlot.unscaledSlotSize);
		}

		public override void ComposeElements(Context ctx, ImageSurface surface)
		{
			Bounds.CalcWorldBounds();
			ComposeSlot();
			ComposeHover();
		}

		private void ComposeHover()
		{
			double slotSize = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)slotSize, (int)slotSize);
			Context context = genContext(imageSurface);
			context.SetSourceRGBA(1.0, 1.0, 1.0, 0.7);
			RoundRectangle(context, 1.0, 1.0, slotSize - 2.0, slotSize - 2.0, GuiStyle.ElementBGRadius);
			context.Fill();
			generateTexture(imageSurface, ref hoverTexture);
			context.Dispose();
			imageSurface.Dispose();
		}

		private void ComposeSlot()
		{
			double slotSize = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			ImageSurface imageSurface = new ImageSurface(Format.Argb32, (int)slotSize, (int)slotSize);
			Context context = genContext(imageSurface);
			context.SetSourceRGBA(1.0, 1.0, 1.0, 0.4);
			RoundRectangle(context, 0.0, 0.0, slotSize, slotSize, GuiStyle.ElementBGRadius);
			context.Fill();
			EmbossRoundRectangleElement(context, 0.0, 0.0, slotSize, slotSize, inverse: true);
			generateTexture(imageSurface, ref slotTexture);
			context.Dispose();
			imageSurface.Dispose();
		}

		public override void RenderInteractiveElements(float deltaTime)
		{
			double pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
			double slotSize = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			int ox = api.Input.MouseX - (int)Bounds.absX;
			int oy = api.Input.MouseY - (int)Bounds.absY;
			float guiScale = RuntimeEnv.GUIScale;
			for(int i = 0; i < rows * cols; i++)
			{
				if(skillItems.Count <= i)
				{
					continue;
				}

				SkillItem skillItem = skillItems[i];
				if(skillItem == null)
				{
					continue;
				}

				int row = i / cols;
				double x = (i % cols) * (slotSize + pad);
				double y = row * (slotSize + pad);
				bool isHovered = ox >= x && oy >= y && ox < x + slotSize + pad && oy < y + slotSize + pad;

				ElementBounds elementBounds = ElementBounds.Fixed((Bounds.renderX + x + 1.0) / guiScale, (Bounds.renderY + y + 1.0) / guiScale, GuiElementPassiveItemSlot.unscaledSlotSize - 2.0, GuiElementPassiveItemSlot.unscaledSlotSize - 2.0).WithParent(api.Gui.WindowBounds);
				elementBounds.CalcWorldBounds();
				api.Render.PushScissor(elementBounds, stacking: true);

				api.Render.Render2DTexture(slotTexture.TextureId, (float)(Bounds.renderX + x), (float)(Bounds.renderY + y), (float)slotSize, (float)slotSize);
				if(isHovered || i == selectedIndex)
				{
					api.Render.Render2DTexture(hoverTexture.TextureId, (float)(Bounds.renderX + x), (float)(Bounds.renderY + y), (float)slotSize, (float)slotSize);
					if(isHovered)
					{
						OnSlotOver?.Invoke(i);
					}
				}

				if(skillItem.Texture != null)
				{
					if(skillItem.TexturePremultipliedAlpha)
					{
						api.Render.Render2DTexturePremultipliedAlpha(skillItem.Texture.TextureId, Bounds.renderX + x + 1.0, Bounds.renderY + y + 1.0, slotSize, slotSize);
					}
					else
					{
						api.Render.Render2DTexture(skillItem.Texture.TextureId, (float)(Bounds.renderX + x + 1.0), (float)(Bounds.renderY + y + 1.0), (float)slotSize, (float)slotSize);
					}
				}

				skillItem.RenderHandler?.Invoke(skillItem.Code, deltaTime, Bounds.renderX + x + 1.0, Bounds.renderY + y + 1.0);
				api.Render.PopScissor();
			}
		}

		public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
		{
			base.OnMouseDownOnElement(api, args);
			int ox = api.Input.MouseX - (int)Bounds.absX;
			int oy = api.Input.MouseY - (int)Bounds.absY;
			double pad = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
			double slotSize = GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
			int row = (int)(oy / (slotSize + pad));
			int col = (int)(ox / (slotSize + pad));
			int index = row * cols + col;
			if(index >= 0 && index < skillItems.Count)
			{
				OnSlotClick?.Invoke(index);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			slotTexture.Dispose();
			hoverTexture.Dispose();
		}
	}

	public static class GuiElementSkillItemGridExt
	{
		public static GuiComposer AddRecipeItemGrid(this GuiComposer composer, List<SkillItem> skillItems, int columns, int rows, Action<int> onSlotClick, ElementBounds bounds, string? key = null)
		{
			if(!composer.Composed)
			{
				composer.AddInteractiveElement(new GuiElementRecipeItemGrid(composer.Api, skillItems, columns, rows, onSlotClick, bounds), key);
			}

			return composer;
		}

		public static GuiElementRecipeItemGrid GetRecipeItemGrid(this GuiComposer composer, string key)
		{
			return (GuiElementRecipeItemGrid)composer.GetElement(key);
		}
	}
}