using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockGlassworktable : BlockHorizontal2BMultiblockMain
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            if(itemstack != null)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassworktable;
                if(be != null)
                {
                    if(be.OnUseItem(byPlayer, slot))
                    {
                        if(world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        }
                        return true;
                    }
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}