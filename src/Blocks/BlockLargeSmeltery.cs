using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockLargeSmeltery : BlockHorizontalStructure
	{
		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
		{
			base.OnNeighbourBlockChange(world, pos, neibpos);
			if(world.BlockAccessor.GetBlockEntity(neibpos) is ITimeBasedHeatSourceContainer container)
			{
				if(world.BlockAccessor.GetBlockEntity(pos) is ITimeBasedHeatReceiver receiver)
				{
					container.SetReceiver(receiver);
				}
			}
		}
	}
}