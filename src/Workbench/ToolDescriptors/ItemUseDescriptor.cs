using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public class ItemUseDescriptor : IWorkbenchToolDescriptor
	{
		public void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, WorkbenchRecipe recipe, int stepIndex,
			CraftingRecipeIngredient? ingredient, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			if(outComponents.Count > 0 && outComponents[^1] is not ClearFloatTextComponent)
			{
				outComponents.Add(new ClearFloatTextComponent(capi));
			}
			var element = new ItemstackTextComponent(capi, ingredient!.ResolvedItemStack, 40.0, 0.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
			element.ShowStacksize = ingredient.ResolvedItemStack!.StackSize > 1;
			outComponents.Add(element);
			outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Hold in your hands"), CairoFont.WhiteSmallText()));
			outComponents.Add(new ClearFloatTextComponent(capi));
		}

		public bool ResolveIngredient(IWorldAccessor world, WorkbenchRecipe recipe, CraftingRecipeIngredient? ingredient, string sourceForErrorLogging)
		{
			if(ingredient == null)
			{
				world.Logger.Log(EnumLogType.Warning, "The item must be specified");
				return false;
			}
			return ingredient.Resolve(world, sourceForErrorLogging);
		}
	}
}