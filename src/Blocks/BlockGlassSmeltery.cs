using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockGlassSmeltery : HeatedBlockBase
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            if(itemstack != null)
            {
                //var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
                //if(be != null)
                //{
                //    if(be.OnInteract(byPlayer, slot, (!byPlayer.Entity.Controls.Sneak) ? 1 : 5))//TODO: try add fuel
                //    {
                //        if(world.Side == EnumAppSide.Client)
                //        {
                //            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                //        }
                //    }
                //}
            }
            return true;
        }
    }
}