using GlassMaking.Workbench;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	public abstract class ItemBehaviorWorkbenchTool : CollectibleBehavior, IItemWorkbenchTool
	{
		protected Cuboidf[] toolBoundingBoxes = null;

		protected ItemBehaviorWorkbenchTool(CollectibleObject collObj) : base(collObj)
		{
		}

		public abstract string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes ?? (toolBoundingBoxes = collObj.Attributes?["workbenchToolBounds"].AsObject<Cuboidf[]>());
		}

		public abstract WorkbenchToolItemBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}