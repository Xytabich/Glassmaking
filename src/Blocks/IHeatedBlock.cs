using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public interface IHeatedBlock
    {
        bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, BlockEntityFirebox firebox);
    }
}