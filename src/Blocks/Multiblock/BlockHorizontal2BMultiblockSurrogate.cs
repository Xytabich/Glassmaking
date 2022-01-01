using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockHorizontal2BMultiblockSurrogate : Block
    {
        protected BlockFacing mainSideFace;

        public override void OnLoaded(ICoreAPI api)
        {
            string side;
            if(!Variant.TryGetValue("horizontalorientation", out side))
            {
                side = Variant["side"];
            }
            var face = BlockFacing.FromCode(side);
            mainSideFace = BlockFacing.HORIZONTALS_ANGLEORDER[(face.HorizontalAngleIndex + 3) % 4];
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            if(TryGetMainBlock(world.BlockAccessor, ref pos, out Block block))
            {
                return block.OnPickBlock(world, pos.AddCopy(mainSideFace));
            }
            else
            {
                return null;
            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[0];
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if(TryGetMainBlock(world.BlockAccessor, ref pos, out _))
            {
                world.BlockAccessor.BreakBlock(pos, byPlayer, dropQuantityMultiplier);
            }
            else
            {
                RemoveBlock(world, pos);
            }
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            if(TryGetMainBlock(world.BlockAccessor, ref pos, out Block block))
            {
                return block.GetPlacedBlockInfo(world, pos, forPlayer);
            }
            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            if(TryGetMainBlock(world.BlockAccessor, ref pos, out Block block))
            {
                return block.GetPlacedBlockName(world, pos);
            }
            return base.GetPlacedBlockName(world, pos);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if(TryGetMainBlock(world.BlockAccessor, ref selection, out Block block))
            {
                return block.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            if(TryGetMainBlock(capi.World.BlockAccessor, ref pos, out Block block))
            {
                return block.GetRandomColor(capi, pos, facing, rndIndex);
            }
            return base.GetRandomColor(capi, pos, facing, rndIndex);
        }

        public void RemoveBlock(IWorldAccessor world, BlockPos pos)
        {
            if(EntityClass != null)
            {
                world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
            }
            world.BlockAccessor.SetBlock(0, pos);
        }

        protected bool TryGetMainBlock(IBlockAccessor accessor, ref BlockSelection selection, out Block block)
        {
            BlockPos pos = selection.Position;
            if(TryGetMainBlock(accessor, ref pos, out block))
            {
                selection = selection.Clone();
                selection.Position = pos;
                return true;
            }
            return false;
        }

        protected bool TryGetMainBlock(IBlockAccessor accessor, ref BlockPos pos, out Block block)
        {
            block = accessor.GetBlock(pos.AddCopy(mainSideFace));
            if(block.Id == 0) return false;
            pos = pos.AddCopy(mainSideFace);
            return true;
        }
    }
}