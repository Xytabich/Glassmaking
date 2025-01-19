using GlassMaking.Common;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.GlassblowingTools
{
	public class DryableTool : GlassblowingToolBehavior
	{
		private string animation = default!;

		public DryableTool(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			animation = properties["animation"].AsString();
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
			if(firstEvent && TryGetRecipeStep(slot, byEntity, out var step, true, true) && slot.Itemstack.Collectible is IWettable wettable)
			{
				if(wettable.GetHumidity(slot.Itemstack, byEntity.World) >= step.StepAttributes!["consume"].AsFloat(0) && step.BeginStep())
				{
					if(api.Side == EnumAppSide.Client) step.SetProgress(0);

					byEntity.AnimManager.StartAnimation(animation);

					handHandling = EnumHandHandling.PreventDefault;
					handling = EnumHandling.PreventSubsequent;
					return;
				}
			}
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(TryGetRecipeStep(slot, byEntity, out var step) && slot.Itemstack.Collectible is IWettable wettable)
			{
				if(step.ContinueStep() && wettable.GetHumidity(slot.Itemstack, byEntity.World) >= step.StepAttributes!["consume"].AsFloat(0))
				{
					float time = step.StepAttributes["time"].AsFloat(1);
					if(api.Side == EnumAppSide.Client)
					{
						step.SetProgress(Math.Max(secondsUsed - 1f, 0f) / time);
					}
					if(byEntity.Api.Side == EnumAppSide.Server && secondsUsed >= time)
					{
						int damage = step.StepAttributes["damage"].AsInt(1);
						if(damage > 0)
						{
							slot.Itemstack.Item.DamageItem(byEntity.World, byEntity, slot, damage);
							slot.MarkDirty();
						}
						float consume = step.StepAttributes["consume"].AsFloat(0);
						if(consume > 0)
						{
							wettable.ConsumeHumidity(slot.Itemstack, consume, byEntity.World);
							slot.MarkDirty();
						}
						step.CompleteStep(byEntity);
						handling = EnumHandling.PreventSubsequent;
						return false;
					}
					handling = EnumHandling.PreventSubsequent;
					return true;
				}
				else
				{
					return false;
				}
			}
			return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(api.Side == EnumAppSide.Client && TryGetRecipeStep(slot, byEntity, out var step))
			{
				if(step.ContinueStep())
				{
					step.SetProgress(0);
				}
			}
			byEntity.AnimManager.StopAnimation(animation);
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
		}
	}
}