using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
    public class BlockFirebox : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            if(itemstack != null)
            {
                BlockEntityFirebox be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFirebox;
                if(be != null)
                {
                    if(itemstack.Class == EnumItemClass.Block && itemstack.Block is IHeaterPlaceableBlock block)
                    {
                        if(block.TryPlaceBlock(world, byPlayer, new BlockSelection { Position = blockSel.Position.UpCopy(), Face = BlockFacing.UP }, itemstack, Variant["side"]))
                        {
                            world.PlaySoundAt(Sounds?.Place, blockSel.Position.X, blockSel.Position.Y + 1, blockSel.Position.Z, byPlayer, true, 16f);
                            if(byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                            {
                                byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                            }
                            be.SetReceiver(world.BlockAccessor.GetBlockEntity(blockSel.Position.UpCopy()) as ITimeBasedHeatReceiver);
                            return true;
                        }
                    }
                    if(be.TryAdd(byPlayer, slot, byPlayer.Entity.Controls.Sprint ? 5 : 1))
                    {
                        if(world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        }
                    }
                }
            }
            return true;
        }

        public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            if(!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox).CanIgnite())
            {
                return EnumIgniteState.NotIgnitablePreventDefault;
            }
            if(!(secondsIgniting > 4f))
            {
                return EnumIgniteState.Ignitable;
            }
            return EnumIgniteState.IgniteNow;
        }

        public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            (byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox)?.TryIgnite();
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var upPos = pos.UpCopy();
            var block = world.BlockAccessor.GetBlock(upPos);
            if(block is IHeaterPlaceableBlock) block.OnBlockBroken(world, upPos, byPlayer, dropQuantityMultiplier);
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var items = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if(items == null) items = new ItemStack[0];
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox;
            if(be != null) items = items.Append(be.GetDropItems() ?? new ItemStack[0]);
            return items;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            if(neibpos == pos.UpCopy())
            {
                var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox;
                if(be != null)
                {
                    be.SetReceiver(world.BlockAccessor.GetBlockEntity(neibpos) as ITimeBasedHeatReceiver);
                }
            }
        }
    }
}