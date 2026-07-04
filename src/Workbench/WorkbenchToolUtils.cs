using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GlassMaking.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

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

		public static bool TryGetTool([NotNullWhen(true)] CollectibleObject? collectible, [NotNullWhen(true)] out ICollectibleWorkbenchTool? tool)
		{
			if(collectible == null)
			{
				tool = null;
				return false;
			}

			if((tool = collectible as ICollectibleWorkbenchTool) != null) return true;
			foreach(var beh in collectible.CollectibleBehaviors)
			{
				if((tool = beh as ICollectibleWorkbenchTool) != null) return true;
			}
			return false;
		}

		public static IReadOnlyDictionary<string, IReadOnlyList<ItemStack>> GetItemsByToolCode(ICoreClientAPI capi)
		{
			return ObjectCacheUtil.GetOrCreate(capi, "glassmaking:workbench-toolitemsbycode", () => {
				var itemsByToolCode = new Dictionary<string, IReadOnlyList<ItemStack>>();
				foreach(var obj in capi.World.BlockItemEnumerator())
				{
					if(IsTool(obj))
					{
						var list = obj.GetHandBookStacks(capi);
						if(list != null)
						{
							foreach(var item in list)
							{
								if(TryGetTool(item.Collectible, out var tool))
								{
									var code = tool.GetToolCode(capi.World, item);
									if(!itemsByToolCode.TryGetValue(code, out var items))
									{
										itemsByToolCode[code] = items = new List<ItemStack>();
									}
									((List<ItemStack>)items).Add(item);
								}
							}
						}
					}
				}
				return itemsByToolCode;
			});
		}
	}
}