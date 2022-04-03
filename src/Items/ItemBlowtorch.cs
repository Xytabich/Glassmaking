using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Common;

namespace GlassMaking.Items
{
	public class ItemBlowtorch : ItemWorkbenchTool
	{
		public override WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity)
		{
			return new BlowtorchToolBehavior(blockentity, GetToolBoundingBoxes(world, itemStack));
		}

		public override string GetToolCode(IWorldAccessor world, ItemStack itemStack)
		{
			return BlowtorchToolBehavior.CODE;
		}
	}
}