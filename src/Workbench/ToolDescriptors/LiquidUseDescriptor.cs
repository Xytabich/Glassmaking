using System.Collections.Generic;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public class LiquidUseDescriptor : IWorkbenchToolDescriptor
	{
		public void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, WorkbenchRecipe recipe, int stepIndex,
			CraftingRecipeIngredient? ingredient, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var itemStack = ingredient!.ResolvedItemStack!;
			itemStack.StackSize = (int)(BlockLiquidContainerBase.GetContainableProps(itemStack)!.ItemsPerLitre * LiquidUseBehavior.RequiresLitres(ingredient));

			var element = new SlideshowItemstackTextComponent(capi, [itemStack], 40.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
			element.ShowStackSize = itemStack.StackSize > 1;
			outComponents.Add(element);
			outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Hold in your hands"), CairoFont.WhiteSmallText()));
		}

		public bool ResolveIngredient(IWorldAccessor world, WorkbenchRecipe recipe, CraftingRecipeIngredient? ingredient, string sourceForErrorLogging)
		{
			if(ingredient == null)
			{
				world.Logger.Log(EnumLogType.Warning, "The liquid must be specified");
				return false;
			}
			if(!ingredient.Resolve(world, sourceForErrorLogging))
			{
				return false;
			}
			if(!(ingredient.RecipeAttributes?.KeyExists("requiresLitres") ?? false))
			{
				world.Logger.Log(EnumLogType.Warning, "The requiresLitres should be spicified");
				return false;
			}
			return true;
		}
	}
}