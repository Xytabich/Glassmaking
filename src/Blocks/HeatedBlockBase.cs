using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class HeatedBlockBase : Block, IHeaterPlaceableBlock
    {
        public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, string side)
        {
            if(world.BlockAccessor.GetBlock(blockSel.Position.UpCopy()).IsReplacableBy(itemstack.Block))
            {
                world.GetBlock(CodeWithVariant("side", side)).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
            return false;
        }
    }
}