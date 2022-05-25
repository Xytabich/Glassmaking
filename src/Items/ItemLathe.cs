using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Common;

namespace GlassMaking.Items
{
	public class ItemLathe : ItemWorkbenchTool, IWorkbenchCustomRenderer
	{
		public override string GetToolCode(IWorldAccessor world, ItemStack itemStack)
		{
			return LatheToolBehavior.CODE;
		}

		public override WorkbenchMountedToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity)
		{
			return new LatheToolBehavior(blockentity, GetToolBoundingBoxes(world, itemStack));
		}
	}
}