using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.GlassblowingTools
{
	public class ToolUse : GlassblowingToolBehavior
	{
		public int minTier;

		protected ModelTransform transform;
		protected ModelTransform animationTransform;
		protected float animationSpeed;

		public ToolUse(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			minTier = properties["minTier"].AsInt(5);
			transform = properties["transform"].AsObject<ModelTransform>()?.EnsureDefaultValues() ?? ModelTransform.NoTransform;
			animationTransform = properties["animation"].AsObject<ModelTransform>()?.EnsureDefaultValues() ?? transform;
			animationSpeed = properties["speed"].AsFloat(0);
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
			if(firstEvent && slot.Itemstack.Collectible.ToolTier >= minTier && TryGetRecipeStep(slot, byEntity, out var step, true, true))
			{
				if(step.BeginStep())
				{
					if(api.Side == EnumAppSide.Client) step.SetProgress(0);
					handHandling = EnumHandHandling.PreventDefault;
					handling = EnumHandling.PreventSubsequent;
					return;
				}
			}
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(slot.Itemstack.Collectible.ToolTier >= minTier && TryGetRecipeStep(slot, byEntity, out var step))
			{
				if(step.ContinueStep())
				{
					if(byEntity.Api.Side == EnumAppSide.Client)
					{
						const float speed = 2f;
						ModelTransform leftTransform = new ModelTransform();
						leftTransform.EnsureDefaultValues();
						leftTransform.Origin.Set(0f, 0f, 0f);
						leftTransform.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 3f);
						leftTransform.Rotation.Y = Math.Min(25f, secondsUsed * 45f * speed);
						leftTransform.Rotation.X = GameMath.Lerp(0f, 0.5f * GameMath.Clamp(byEntity.Pos.Pitch - (float)Math.PI, -0.2f, 1.0995574f) * GameMath.RAD2DEG, Math.Min(1, secondsUsed * speed * 4f));
						leftTransform.Rotation.Z = secondsUsed * 90f % 360f;
						byEntity.Controls.LeftUsingHeldItemTransformBefore = leftTransform;

						ModelTransform rightTransform = new ModelTransform();
						rightTransform.EnsureDefaultValues();
						float pt = GameMath.Min(secondsUsed * speed * 1.5f, 1f);
						float at = GameMath.FastSin(secondsUsed * animationSpeed) * 0.5f + 0.5f;
						rightTransform.Lerp(transform, pt).Lerp(animationTransform, at);
						byEntity.Controls.UsingHeldItemTransformBefore = rightTransform;

						byEntity.Controls.HandUse = EnumHandInteract.None;
					}

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
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
		}
	}
}