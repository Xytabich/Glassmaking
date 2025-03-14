﻿using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockEntityLargeSmelteryHearth : BlockEntity, IHeatSourceModifier, ITimeBasedHeatReceiver, IGlassmeltSource
	{
		public float FuelRateModifier => smelteryCore?.FuelRateModifier ?? 1;
		public float TemperatureModifier => smelteryCore?.TemperatureModifier ?? 1;

		private BlockEntityLargeSmelteryCore? smelteryCore = null;
		private ITimeBasedHeatSourceControl? heatSource = null;

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			smelteryCore = api.World.BlockAccessor.GetBlockEntity(((BlockLargeSmelteryHearth)Block).GetMainBlockPosition(Pos)) as BlockEntityLargeSmelteryCore;
		}

		public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int multiplier)
		{
			return smelteryCore?.TryAdd(byPlayer, slot, multiplier) ?? false;
		}

		public void GetGlassFillState(out int canAddAmount, out AssetLocation? code)
		{
			if(smelteryCore == null)
			{
				canAddAmount = 0;
				code = null;
			}
			else
			{
				smelteryCore.GetGlassFillState(out canAddAmount, out code);
			}
		}

		public ItemStack[] GetDropItems()
		{
			return smelteryCore?.GetDropItems() ?? new ItemStack[0];
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();

			heatSource = null;
			smelteryCore?.SetHeater(BlockFacing.FromCode(Block.Variant["side"]).HorizontalAngleIndex, null);
			smelteryCore = null;
		}

		// Used if Initialize is called at different times, or the core block is unloaded or loaded in a neighboring chunk
		public void OnCoreUpdated(BlockEntityLargeSmelteryCore? smelteryCore)
		{
			this.smelteryCore = smelteryCore;
			smelteryCore?.SetHeater(BlockFacing.FromCode(Block.Variant["side"]).HorizontalAngleIndex, heatSource);
		}

		void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSourceControl? heatSource)
		{
			this.heatSource = heatSource;
			smelteryCore?.SetHeater(BlockFacing.FromCode(Block.Variant["side"]).HorizontalAngleIndex, heatSource);
		}

		bool IGlassmeltSource.CanInteract(EntityAgent byEntity, BlockSelection blockSel)
		{
			return true;
		}

		float IGlassmeltSource.GetTemperature()
		{
			return smelteryCore?.GetTemperature() ?? 20;
		}

		int IGlassmeltSource.GetGlassAmount()
		{
			return smelteryCore?.GetGlassAmount() ?? 0;
		}

		AssetLocation? IGlassmeltSource.GetGlassCode()
		{
			return smelteryCore?.GetGlassCode();
		}

		void IGlassmeltSource.RemoveGlass(int amount)
		{
			smelteryCore!.RemoveGlass(amount);
		}

		void IGlassmeltSource.SpawnMeltParticles(IWorldAccessor world, BlockSelection blockSel, IPlayer? byPlayer, float quantity)
		{
			smelteryCore!.SpawnMeltParticles(world, blockSel, byPlayer, quantity);
		}
	}
}