using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public class ItemToolDescriptor : IWorkbenchToolDescriptor
	{
		protected string ToolCode { get; }

		public ItemToolDescriptor(string toolCode)
		{
			ToolCode = toolCode;
		}

		public void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, WorkbenchRecipe recipe, int stepIndex, CraftingRecipeIngredient? ingredient, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var toolItems = WorkbenchToolUtils.GetItemsByToolCode(capi);
			if(toolItems.TryGetValue(ToolCode, out var list))
			{
				var element = new SlideshowItemstackTextComponent(capi, list.ToArray(), 40.0, EnumFloat.Inline,
					cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
				outComponents.Add(element);
			}
		}

		public virtual bool ResolveIngredient(IWorldAccessor world, WorkbenchRecipe recipe, CraftingRecipeIngredient? ingredient, string sourceForErrorLogging)
		{
			return true;
		}
	}
}