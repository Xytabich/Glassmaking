using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	public abstract class ItemWorkbenchTool : Item, ICollectibleWorkbenchTool
	{
		protected Cuboidf[] toolBoundingBoxes = null!;

		public abstract string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes ??= Attributes?["workbenchToolBounds"].AsObject<Cuboidf[]>()!;
		}

		public abstract WorkbenchMountedToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}