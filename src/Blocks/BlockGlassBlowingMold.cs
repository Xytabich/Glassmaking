using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockGlassBlowingMold : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if(blockSel != null)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassBlowingMold;
                if(be != null)
                {
                    if(be.OnInteract(world, byPlayer))
                    {
                        return false;
                    }
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}