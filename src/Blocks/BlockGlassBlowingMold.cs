using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

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
                        return true;
                    }
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var items = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if(items == null) items = new ItemStack[0];
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityGlassBlowingMold;
            if(be != null) items = items.Append(be.GetDropItems() ?? new ItemStack[0]);
            return items;
        }
    }
}