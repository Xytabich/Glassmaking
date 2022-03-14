using Vintagestory.API.Common;

namespace GlassMaking.Blocks.Multiblock
{
	public interface IStructurePlanMainBlock
	{
		void OnSurrogateReplaced(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Block oldBlock, Block newBlock);
	}
}