using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	public interface IItemWorkbenchTool
	{
		string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack);

		WorkbenchToolItemBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}