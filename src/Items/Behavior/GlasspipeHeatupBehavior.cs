using GlassMaking.Blocks;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items.Behavior
{
	public class GlasspipeHeatupBehavior : CollectibleBehavior, IPrioritizedBehavior
	{
		private const string LASTHEAT_ATTRIB = "glassmaking:lastHeatTime";

		public double Priority => 0.3;

		private ICoreAPI api;
		private GlassMakingMod glassMaking;
		private readonly ItemGlassworkPipe glassworkPipe;
		private string animation;

		public GlasspipeHeatupBehavior(CollectibleObject collObj) : base(collObj)
		{
			glassworkPipe = (ItemGlassworkPipe)collObj;
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			animation = properties["animation"].AsString();
		}

		public override void OnLoaded(ICoreAPI api)
		{
			this.api = api;
			glassMaking = api.ModLoader.GetModSystem<GlassMakingMod>();
		}

		public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
		{
			if(glassworkPipe.GetActiveCraft(inSlot.Itemstack) != null)
			{
				return new WorldInteraction[] {
					new WorldInteraction() {
						ActionLangCode = "glassmaking:heldhelp-glasspipe-heatup",
						MouseButton = EnumMouseButton.Right,
						HotKeyCode = "sprint",
						Itemstacks = Utils.GetGlassmeltSources(api)
					}
				};
			}
			return Array.Empty<WorldInteraction>();
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
			if(firstEvent && blockSel != null)
			{
				var itemstack = slot.Itemstack;
				var activeCraft = glassworkPipe.GetActiveCraft(itemstack);
				if(activeCraft != null)
				{
					var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IGlassmeltSource;
					if(source != null && source.CanInteract(byEntity, blockSel))
					{
						if(byEntity.Controls.Sprint)
						{
							float temperature = glassworkPipe.GetGlassTemperature(byEntity.World, itemstack);
							float temp = source.GetTemperature();
							if(temp > temperature)
							{
								if(activeCraft.IsHeated(byEntity.World, itemstack))
								{
									itemstack.TempAttributes.SetFloat(LASTHEAT_ATTRIB, 0f);

									byEntity.AnimManager.StartAnimation(animation);
								}
								else if(api.Side == EnumAppSide.Client)
								{
									((ICoreClientAPI)api).TriggerIngameError(this, "toocold", Lang.Get("glassmaking:Unable to reheat a cold workpiece"));
								}
							}
							handHandling = EnumHandHandling.PreventDefault;
							handling = EnumHandling.PreventSubsequent;
						}
					}
				}
			}
		}

		public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(blockSel == null) return false;

			var itemstack = slot.Itemstack;
			var activeCraft = glassworkPipe.GetActiveCraft(itemstack);
			if(activeCraft == null) return false;

			var source = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as IGlassmeltSource;
			if(source != null && source.CanInteract(byEntity, blockSel))
			{
				if(byEntity.Controls.Sprint)
				{
					float temperature = glassworkPipe.GetGlassTemperature(byEntity.World, itemstack);
					float temp = source.GetTemperature();
					if(temp > temperature)
					{
						if(activeCraft.IsHeated(byEntity.World, itemstack))
						{
							if(api.Side == EnumAppSide.Client)
							{
								const float speed = 1.5f;
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
							if(api.Side == EnumAppSide.Server && itemstack.TempAttributes.GetFloat(LASTHEAT_ATTRIB) + 1f <= secondsUsed)
							{
								itemstack.TempAttributes.SetFloat(LASTHEAT_ATTRIB, (float)Math.Floor(secondsUsed));
								glassworkPipe.SetGlassTemperature(byEntity.World, itemstack, GameMath.Min(temp, temperature + 100));
								slot.MarkDirty();
							}

							handling = EnumHandling.PreventSubsequent;
							return true;
						}
					}
				}
			}
			return false;
		}

		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			var itemstack = slot.Itemstack;
			if(itemstack.TempAttributes.HasAttribute(LASTHEAT_ATTRIB))
			{
				handling = EnumHandling.PreventSubsequent;
				itemstack.TempAttributes.RemoveAttribute(LASTHEAT_ATTRIB);
				byEntity.AnimManager.StopAnimation(animation);
			}
		}

		public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handling)
		{
			var itemstack = slot.Itemstack;
			if(itemstack.TempAttributes.HasAttribute(LASTHEAT_ATTRIB))
			{
				handling = EnumHandling.PreventSubsequent;
				itemstack.TempAttributes.RemoveAttribute(LASTHEAT_ATTRIB);
				byEntity.AnimManager.StopAnimation(animation);
			}
			return true;
		}
	}
}