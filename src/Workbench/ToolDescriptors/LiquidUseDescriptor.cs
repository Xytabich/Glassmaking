using GlassMaking.Workbench.ToolBehaviors;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public class LiquidUseDescriptor : IWorkbenchToolDescriptor
	{
		public void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, WorkbenchRecipe recipe, int stepIndex, JsonObject? json,
			ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var ingredient = json?.AsObject<LiquidUseBehavior.RequiredLiquid?>(null, recipe.Code.Domain);
			if(ingredient == null)
			{
				capi.World.Logger.Log(EnumLogType.Warning, "Unable to use liquid in workbench recipe '{0}' because json is malformed", recipe.Code);
			}
			else
			{
				var itemStack = ingredient.Type == EnumItemClass.Item ? new ItemStack(capi.World.GetItem(ingredient.Code)) : new ItemStack(capi.World.GetBlock(ingredient.Code));
				itemStack.StackSize = (int)(BlockLiquidContainerBase.GetContainableProps(itemStack).ItemsPerLitre * ingredient.RequiresLitres);

				var element = new SlideshowItemstackTextComponent(capi, new ItemStack[] { itemStack }, 40.0, EnumFloat.Inline,
					cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
				element.ShowStackSize = itemStack.StackSize > 1;
				outComponents.Add(element);
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Hold in your hands"), CairoFont.WhiteSmallText()));
			}
		}
	}
}