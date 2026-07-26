using System;
using System.Collections.Generic;
using GlassMaking.Common;
using GlassMaking.Entities.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Items.Behavior
{
	public class GlassShatteringBehavior : CollectibleBehavior, IItemEntityTickListener
	{
		private ICoreAPI api = default!;
		private float shatterTemperature;
		private Dictionary<AssetLocation, int> glassAmount = default!;

		public GlassShatteringBehavior(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void Initialize(JsonObject properties)
		{
			base.Initialize(properties);
			shatterTemperature = properties["threshold"].AsFloat(100f);
			glassAmount = properties["glass"].AsObject<Dictionary<AssetLocation, int>>(null, collObj.Code.Domain) ?? new();
		}

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			this.api = api;
		}

		public void OnGameTick(EntityItem entity, float deltaTime)
		{
			if(!entity.Swimming || api.Side != EnumAppSide.Server)
			{
				return;
			}

			if(!CoolToTemperature(entity, entity.Slot, entity.Pos.XYZ, deltaTime, GlobalConstants.CollectibleDefaultTemperature))
			{
				entity.Die(EnumDespawnReason.Removed);
			}
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
		{
			if(blockSel != null && IsCoolingMedium(blockSel, slot, blockSel.FullPosition))
			{
				handHandling = EnumHandHandling.PreventDefault;
				handling = EnumHandling.PreventDefault;
				return;
			}
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
		}

		public override bool OnHeldInteractStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
		{
			if(blockSel != null && IsCoolingMedium(blockSel, slot, blockSel.FullPosition))
			{
				CoolToTemperature(byEntity, slot, blockSel.FullPosition, 0.02f, GlobalConstants.CollectibleDefaultTemperature);
				handling = EnumHandling.PreventDefault;
				return slot.Itemstack != null;
			}
			return base.OnHeldInteractStep(secondsPassed, slot, byEntity, blockSel, entitySel, ref handling);
		}

		private bool IsCoolingMedium(BlockSelection blockSel, ItemSlot slot, Vec3d pos)
		{
			var coolingMedium = api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.FluidOrSolid)
				.GetInterface<ICoolingMedium>(api.World, blockSel.Position);
			if(coolingMedium != null) return coolingMedium.CanCool(slot, pos);

			var insideBlockPos = blockSel.Position.AddCopy(blockSel.Face.Normali);
			coolingMedium = api.World.BlockAccessor.GetBlock(insideBlockPos, BlockLayersAccess.FluidOrSolid)
			   .GetInterface<ICoolingMedium>(api.World, insideBlockPos);
			return coolingMedium?.CanCool(slot, pos) ?? false;
		}

		private bool CoolToTemperature(Entity entity, ItemSlot slot, Vec3d effectsPos, float dt, float targetTemperature)
		{
			float stackTemp = slot.Itemstack!.Collectible.GetTemperature(api.World, slot.Itemstack);
			if(stackTemp <= targetTemperature)
			{
				return false;
			}

			if(stackTemp >= shatterTemperature && api.World.Rand.NextDouble() < 0.01)
			{
				Shatter(entity, slot);
				return false;
			}

			float nextTemperature = Math.Max(GlobalConstants.CollectibleDefaultTemperature, stackTemp - 200f * dt);
			slot.Itemstack!.Collectible.SetTemperature(api.World, slot.Itemstack, nextTemperature);

			float tempDiff = stackTemp - targetTemperature;
			if(tempDiff > 90f && api.World.Rand.NextDouble() < 0.5)
			{
				Entity.SplashParticleProps.BasePos.Set(effectsPos.X, effectsPos.Y - 0.75, effectsPos.Z);
				Entity.SplashParticleProps.AddVelocity.Set(0f, 0f, 0f);
				Entity.SplashParticleProps.QuantityMul = 0.1f;
				api.World.SpawnParticles(Entity.SplashParticleProps);
			}
			return true;
		}

		private void Shatter(Entity entity, ItemSlot slot)
		{
			var stackSize = slot.StackSize;
			var pos = entity.Pos;
			api.World.PlaySoundAt(new("sounds/block/glass"), pos.X, pos.Y, pos.Z, null, true, 16f);
			var temperature = slot.Itemstack!.Collectible.GetTemperature(api.World, slot.Itemstack);

			var mod = api.ModLoader.GetModSystem<GlassMakingMod>();
			foreach(var (code, amount) in glassAmount)
			{
				foreach(var item in mod.GetShardsList(api.World, code, amount * stackSize))
				{
					item.Collectible.SetTemperature(api.World, item, temperature);
					api.World.SpawnItemEntity(item, pos.XYZ)?.Pos.SetFrom(pos);
				}
			}

			slot.Itemstack = null;
			slot.MarkDirty();
		}
	}
}