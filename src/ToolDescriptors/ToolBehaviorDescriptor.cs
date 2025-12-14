using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.ToolDescriptors
{
	public abstract class ToolBehaviorDescriptor<T> : ToolBehaviorDescriptor, IPipeBlowingToolDescriptor where T : GlassblowingToolBehavior
	{
		protected readonly GlassMakingMod mod;

		protected Dictionary<string, ItemStack[]> handbookItemsByType = default!;

		public ToolBehaviorDescriptor(GlassMakingMod mod)
		{
			this.mod = mod;
		}

		public override void OnLoaded(ICoreAPI api)
		{
			var tools = ToolCollection.Create(api);
			foreach(var item in api.World.BlockItemEnumerator())
			{
				foreach(var beh in item.CollectibleBehaviors)
				{
					if(IsSuitableBehavior(item, beh))
					{
						var code = ((GlassblowingToolBehavior)beh).ToolCode;
						mod.AddPipeToolDescriptor(code, this);
						tools?.AddItem(code, item);
					}
				}
			}
			handbookItemsByType = tools?.Collect()!;
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

		public virtual void GetWildcardMapping(IWorldAccessor world, GlassBlowingRecipe recipe, int stepIndex, Dictionary<string, string[]> outMap)
		{

		}

		public abstract void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents);

		public abstract void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo);

		public abstract void GetInteractionHelp(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, List<WorldInteraction> interactions);

		protected virtual bool IsSuitableBehavior(CollectibleObject item, CollectibleBehavior beh)
		{
			return beh.GetType() == typeof(T);
		}

		protected class ToolCollection
		{
			private readonly ICoreClientAPI capi;
			private readonly Dictionary<string, List<ItemStack>> byType = new();

			public ToolCollection(ICoreClientAPI capi)
			{
				this.capi = capi;
			}

			public void AddItem(string code, CollectibleObject item)
			{
				if(!byType.TryGetValue(code, out var list))
				{
					byType[code] = list = new List<ItemStack>();
				}
				List<ItemStack> stacks = item.GetHandBookStacks(capi);
				if(stacks != null) list.AddRange(stacks);
			}

			public Dictionary<string, ItemStack[]> Collect()
			{
				var handbookItemsByType = new Dictionary<string, ItemStack[]>(byType.Count);
				foreach(var pair in byType)
				{
					handbookItemsByType[pair.Key] = pair.Value.ToArray();
				}
				return handbookItemsByType;
			}

			public static ToolCollection? Create(ICoreAPI api)
			{
				if(api is ICoreClientAPI capi)
				{
					return new ToolCollection(capi);
				}
				return null;
			}
		}
	}

	public abstract class ToolBehaviorDescriptor
	{
		public abstract void OnLoaded(ICoreAPI api);

		public abstract void OnUnloaded();
	}
}