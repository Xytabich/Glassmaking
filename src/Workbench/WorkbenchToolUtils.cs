using Vintagestory.API.Common;

namespace GlassMaking.Workbench
{
	public static class WorkbenchToolUtils
	{
		public static bool IsTool(CollectibleObject collectible)
		{
			if(collectible is IItemWorkbenchTool) return true;
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if(beh is IItemWorkbenchTool) return true;
			}
			return false;
		}

		public static bool TryGetTool(CollectibleObject collectible, out IItemWorkbenchTool tool)
		{
			if((tool = collectible as IItemWorkbenchTool) != null) return true;
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if((tool = beh as IItemWorkbenchTool) != null) return true;
			}
			return false;
		}
	}
}