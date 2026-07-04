using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public interface IWorkbenchToolDescriptor
	{
		void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, WorkbenchRecipe recipe, int stepIndex,
			CraftingRecipeIngredient? ingredient, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents);

		bool ResolveIngredient(IWorldAccessor world, WorkbenchRecipe recipe, CraftingRecipeIngredient? ingredient, string sourceForErrorLogging);
	}
}