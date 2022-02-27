using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockEntityLargeSmelteryHearth : BlockEntity, IBurnerModifier, ITimeBasedHeatReceiver
	{
		public float DurationModifier => smelteryCore?.DurationModifier ?? 1;
		public float TemperatureModifier => smelteryCore?.TemperatureModifier ?? 1;

		private BlockEntityLargeSmelteryCore smelteryCore = null;
		private ITimeBasedHeatSource heatSource = null;

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			smelteryCore = api.World.BlockAccessor.GetBlockEntity(((BlockLargeSmelteryHearth)Block).GetMainBlockPosition(Pos)) as BlockEntityLargeSmelteryCore;
		}

		public bool TryAdd(IPlayer byPlayer, ItemSlot slot, int multiplier)
		{
			return smelteryCore?.TryAdd(byPlayer, slot, multiplier) ?? false;
		}

		public void GetGlassFillState(out int canAddAmount, out AssetLocation code)
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

		public void SetHeatSource(ITimeBasedHeatSource heatSource)
		{
			this.heatSource = heatSource;
			smelteryCore?.SetHeater(BlockFacing.FromCode(Block.Variant["side"]).HorizontalAngleIndex, heatSource);
		}

		public void OnHeatSourceTick(float dt)
		{
			smelteryCore?.OnHeatTick(heatSource, dt);
		}
	}
}