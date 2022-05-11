using GlassMaking.Workbench;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	public abstract class ItemWorkbenchTool : Item, IItemWorkbenchTool
	{
		protected Cuboidf[] toolBoundingBoxes = null;

		public abstract string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes ?? (toolBoundingBoxes = Attributes?["workbenchToolBounds"].AsObject<Cuboidf[]>());
		}

		public abstract WorkbenchToolItemBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}