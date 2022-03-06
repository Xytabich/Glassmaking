using GlassMaking.Blocks;
using GlassMaking.Items;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.GlassblowingTools
{
	public class GlassIntakeTool : GlassblowingToolBehavior
	{
		public GlassIntakeTool(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
			if(firstEvent && blockSel != null && TryGetRecipeStep(slot, byEntity, out var step, true, true))
			{
				var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IGlassmeltSource;
				if(source != null && source.CanInteract(byEntity, blockSel))
				{
					int sourceAmount = source.GetGlassAmount();
					var sourceGlassCode = source.GetGlassCode();
					if(sourceAmount > 0 && sourceGlassCode.Equals(new AssetLocation(step.StepAttributes["code"].AsString())))
					{
						if(step.BeginStep())
						{
							int intake = slot.Itemstack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0);
							if(sourceAmount > 0 && intake < step.StepAttributes["amount"].AsInt())
							{
								if(byEntity.World.Side == EnumAppSide.Server)
								{
									slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", 0f);
								}
								slot.Itemstack.TempAttributes.SetBool("glassmaking:intakeStarted", true);
								handHandling = EnumHandHandling.PreventDefault;
								handling = EnumHandling.PreventSubsequent;
							}
						}
					}
				}
			}
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(blockSel != null && TryGetRecipeStep(slot, byEntity, out var step))
			{
				if(slot.Itemstack.TempAttributes.GetBool("glassmaking:intakeStarted", false) && step.ContinueStep())
				{
					var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IGlassmeltSource;
					if(source != null && source.CanInteract(byEntity, blockSel))
					{
						int sourceAmount = source.GetGlassAmount();
						var sourceGlassCode = source.GetGlassCode();
						if(sourceAmount > 0 && sourceGlassCode.Equals(new AssetLocation(step.StepAttributes["code"].AsString())))
						{
							int intake = slot.Itemstack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0);
							int amount = step.StepAttributes["amount"].AsInt();
							if(intake < amount)
							{
								const float speed = 1.5f;
								if(byEntity.Api.Side == EnumAppSide.Client)
								{
									ModelTransform modelTransform = new ModelTransform();
									modelTransform.EnsureDefaultValues();
									modelTransform.Origin.Set(0f, 0f, 0f);
									modelTransform.Translation.Set(-Math.Min(0.5f, speed * secondsUsed), -Math.Min(0.5f, speed * secondsUsed), Math.Min(0.5f, speed * secondsUsed));
									modelTransform.Scale = 1f - Math.Min(0.1f, speed * secondsUsed / 4f);
									modelTransform.Rotation.X = -Math.Min(10f, secondsUsed * 45f * speed);
									modelTransform.Rotation.Y = -Math.Min(15f, secondsUsed * 45f * speed) + GameMath.FastSin(secondsUsed * 1.5f);
									modelTransform.Rotation.Z = secondsUsed * 90f % 360f;
									byEntity.Controls.UsingHeldItemTransformBefore = modelTransform;
								}
								const float useTime = 2f;
								if(byEntity.Api.Side == EnumAppSide.Server && secondsUsed >= useTime)
								{
									if(slot.Itemstack.TempAttributes.GetFloat("glassmaking:lastAddGlassTime") + useTime <= secondsUsed)
									{
										slot.Itemstack.TempAttributes.SetFloat("glassmaking:lastAddGlassTime", (float)Math.Floor(secondsUsed));
										int consumed = Math.Min(Math.Min(amount - intake, sourceAmount), (byEntity.Controls.Sneak ? 5 : 1) * (5 + (int)(intake * 0.01f)));

										((ItemGlassworkPipe)slot.Itemstack.Collectible).ChangeGlassTemperature(byEntity.World, slot.Itemstack,
											GetAmountFromPreviousSteps(step) + intake + consumed, consumed, source.GetTemperature());

										intake += consumed;
										source.RemoveGlass(consumed);

										if(intake >= amount)
										{
											slot.Itemstack.Attributes.RemoveAttribute("glassmaking:toolIntakeAmount");
											slot.MarkDirty();

											step.CompleteStep(byEntity);
											handling = EnumHandling.PreventSubsequent;
											return false;
										}

										slot.Itemstack.Attributes.SetInt("glassmaking:toolIntakeAmount", intake);
										step.SetProgress((float)intake / amount);
										slot.MarkDirty();
									}
								}
								if(secondsUsed > 1f / speed)
								{
									IPlayer byPlayer = null;
									if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
									source.SpawnMeltParticles(byEntity.World, blockSel, byPlayer);
								}
								handling = EnumHandling.PreventSubsequent;
								return true;
							}
						}
					}
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
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:lastAddGlassTime");
			slot.Itemstack.TempAttributes.RemoveAttribute("glassmaking:intakeStarted");
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
		}

		private int GetAmountFromPreviousSteps(ToolRecipeStep stepInfo)
		{
			int amount = 0;
			var steps = stepInfo.Recipe.Steps;
			for(int i = 0; i < stepInfo.Index; i++)
			{
				if(steps[i].Tool == ToolCode)
				{
					amount += steps[i].Attributes["amount"].AsInt();
				}
			}
			return amount;
		}
	}
}