using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	public interface ICollectibleWorkbenchTool
	{
		string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack);

		WorkbenchMountedToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}