using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public interface IHeaterPlaceableBlock
	{
		bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, string side);
	}
}