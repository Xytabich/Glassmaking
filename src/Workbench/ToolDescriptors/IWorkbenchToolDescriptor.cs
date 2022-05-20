using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public interface IWorkbenchToolDescriptor
	{
		void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, WorkbenchRecipe recipe, int stepIndex, JsonObject data, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents);
	}
}