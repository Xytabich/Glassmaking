using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockTemperingOven : HeatedBlockBase
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityTemperingOven be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTemperingOven;
            if(be != null)
            {
                if(be.TryInteract(byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot))
                {
                    if(world.Side == EnumAppSide.Client)
                    {
                        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    }
                }
            }
            return true;
        }
    }
}