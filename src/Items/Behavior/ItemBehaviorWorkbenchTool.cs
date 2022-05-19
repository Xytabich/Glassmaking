using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Items.Behavior
{
	public class ItemBehaviorWorkbenchTool : CollectibleBehaviorWorkbenchTool
	{
		private string toolCode;
		private bool isTool;

		public ItemBehaviorWorkbenchTool(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			toolCode = properties["code"].AsString();
			isTool = properties["isTool"].AsBool(false);
		}

		public override string GetToolCode(IWorldAccessor world, ItemStack itemStack)
		{
			return toolCode;
		}

		public override WorkbenchMountedToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity)
		{
			if(isTool)
			{
				return new DamageableToolBehavior(toolCode, blockentity, GetToolBoundingBoxes(world, itemStack));
			}
			return new SimpleToolBehavior(toolCode, blockentity, GetToolBoundingBoxes(world, itemStack));
		}
	}
}
