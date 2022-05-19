using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	public abstract class CollectibleBehaviorWorkbenchTool : CollectibleBehavior, ICollectibleWorkbenchTool
	{
		protected Cuboidf[] toolBoundingBoxes = null;

		protected CollectibleBehaviorWorkbenchTool(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			toolBoundingBoxes = properties["workbenchToolBounds"].AsObject<Cuboidf[]>();
		}

		public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes;
		}

		public abstract string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		public abstract WorkbenchMountedToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}