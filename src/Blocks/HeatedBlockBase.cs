using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class HeatedBlockBase : Block, IHeatedBlock
    {
        public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, BlockEntityFirebox firebox)
        {
            if(world.BlockAccessor.GetBlock(blockSel.Position.UpCopy()).IsReplacableBy(itemstack.Block))
            {
                world.GetBlock(CodeWithVariant("side", firebox.Block.Variant["side"])).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                return true;
            }
            return false;
        }
    }
}