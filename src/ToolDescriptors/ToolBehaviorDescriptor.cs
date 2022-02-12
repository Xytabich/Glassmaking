using GlassMaking.GlassblowingTools;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.ToolDescriptors
{
	public abstract class ToolBehaviorDescriptor<T> : ToolBehaviorDescriptor, IPipeBlowingToolDescriptor where T : GlassblowingToolBehavior
	{
		protected GlassMakingMod mod;

		protected Dictionary<string, ItemStack[]> handbookItemsByType;

		public ToolBehaviorDescriptor(GlassMakingMod mod)
		{
			this.mod = mod;
		}

		public override void OnLoaded(ICoreAPI api)
		{
			var capi = api as ICoreClientAPI;
			Dictionary<string, List<ItemStack>> byType = null;
			if(capi != null)
			{
				byType = new Dictionary<string, List<ItemStack>>();
			}
			foreach(var item in api.World.Collectibles)
			{
				foreach(var beh in item.CollectibleBehaviors)
				{
					if(IsSuitableBehavior(item, beh))
					{
						var code = ((GlassblowingToolBehavior)beh).ToolCode;
						mod.AddPipeToolDescriptor(code, this);
						if(capi != null)
						{
							if(!byType.TryGetValue(code, out var list))
							{
								byType[code] = list = new List<ItemStack>();
							}
							List<ItemStack> stacks = item.GetHandBookStacks(capi);
							if(stacks != null) list.AddRange(stacks);
						}
					}
				}
			}
			if(capi != null)
			{
				handbookItemsByType = new Dictionary<string, ItemStack[]>(byType.Count);
				foreach(var pair in byType)
				{
					handbookItemsByType[pair.Key] = pair.Value.ToArray();
				}
			}
		}

		public override void OnUnloaded()
		{

		}

		public virtual bool TryGetWorkingTemperature(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int currentStepIndex, out float temperature)
		{
			temperature = 0;
			return false;
		}

		public virtual void GetBreakDrops(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int currentStepIndex, List<ItemStack> outList)
		{

		}

		public abstract void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents);

		public abstract void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo);

		protected virtual bool IsSuitableBehavior(CollectibleObject item, CollectibleBehavior beh)
		{
			return beh.GetType() == typeof(T);
		}
	}

	public abstract class ToolBehaviorDescriptor
	{
		public abstract void OnLoaded(ICoreAPI api);

		public abstract void OnUnloaded();
	}
}