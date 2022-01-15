using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockWorkbench : BlockHorizontal2BMultiblock
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            if(itemstack != null)
            {
                blockSel = GetMainBlockSelection(blockSel);
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
                if(be != null)
                {
                    if(be.OnUseItem(byPlayer, slot))
                    {
                        if(world.Side == EnumAppSide.Client)
                        {
                            ((IClientPlayer)byPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        }
                        return true;
                    }
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            pos = GetMainBlockPosition(pos);
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityWorkbench;
            if(be != null)
            {
                var boxes = be.GetSelectionBoxes();
                if(isSurrogate)
                {
                    boxes = (Cuboidf[])boxes.Clone();
                    for(int i = 1; i < boxes.Length; i++)
                    {
                        boxes[i] = boxes[i].OffsetCopy(oppositeFace.Normalf);
                    }
                }
                return boxes;
            }
            return base.GetSelectionBoxes(blockAccessor, pos);
        }
    }
}