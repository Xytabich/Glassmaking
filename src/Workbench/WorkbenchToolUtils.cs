using Vintagestory.API.Common;

namespace GlassMaking.Workbench
{
	public static class WorkbenchToolUtils
	{
		public static bool IsTool(CollectibleObject collectible)
		{
			if(collectible is ICollectibleWorkbenchTool) return true;
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if(beh is ICollectibleWorkbenchTool) return true;
			}
			return false;
		}

		public static bool TryGetTool(CollectibleObject collectible, out ICollectibleWorkbenchTool tool)
		{
			if((tool = collectible as ICollectibleWorkbenchTool) != null) return true;
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if((tool = beh as ICollectibleWorkbenchTool) != null) return true;
			}
			return false;
		}
	}
}