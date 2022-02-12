using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public class HeatedBlockBase : Block, IHeaterPlaceableBlock
	{
		public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, string side)
		{
			if(world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(this))
			{
				return world.GetBlock(CodeWithVariant("side", side)).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			}
			return false;
		}
	}
}