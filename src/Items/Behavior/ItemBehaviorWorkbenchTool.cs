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

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if(string.IsNullOrEmpty(toolCode))
			{
				api.World.Logger.Warning(string.Format("Item {0} does not contain a tool code, specify it for the tool to work correctly.", collObj.Code));
				toolCode = collObj.Code.ToShortString();
			}
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
