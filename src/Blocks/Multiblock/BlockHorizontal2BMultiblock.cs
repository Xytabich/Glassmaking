using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockHorizontal2BMultiblock : Block
    {
        protected bool isSurrogate;
        protected BlockFacing oppositeFace;

        public override void OnLoaded(ICoreAPI api)
        {
            isSurrogate = Attributes == null || !Attributes.KeyExists("surrogate");

            string side;
            if(!Variant.TryGetValue("horizontalorientation", out side))
            {
                side = Variant["side"];
            }
            var face = BlockFacing.FromCode(side);
            oppositeFace = BlockFacing.HORIZONTALS_ANGLEORDER[(face.HorizontalAngleIndex + (isSurrogate ? 3 : 1)) % 4];
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            if(isSurrogate)
            {
                var mainPos = GetMainBlockPosition(pos);
                if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontal2BMultiblock mainBlock)
                {
                    return mainBlock.OnPickBlock(world, mainPos);
                }
                return null;
            }
            else
            {
                return base.OnPickBlock(world, pos);
            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if(isSurrogate) return null;
            return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            if(!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }
            if(isSurrogate) return true;
            var surrogate = new AssetLocation(Attributes["surrogate"].AsString());
            blockSel = blockSel.Clone();
            blockSel.Position = blockSel.Position.AddCopy(oppositeFace);
            return world.GetBlock(surrogate).CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if(base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
            {
                if(isSurrogate) return true;

                blockSel = blockSel.Clone();
                blockSel.Position = blockSel.Position.AddCopy(oppositeFace);
                var surrogate = new AssetLocation(Attributes["surrogate"].AsString());
                world.GetBlock(surrogate).DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
                return true;
            }
            return false;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if(isSurrogate)
            {
                var mainPos = GetMainBlockPosition(pos);
                if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontal2BMultiblock)
                {
                    world.BlockAccessor.BreakBlock(mainPos, byPlayer, dropQuantityMultiplier);
                }
                else
                {
                    RemoveSurrogateBlock(world, pos);
                }
            }
            else
            {
                var spos = pos.AddCopy(oppositeFace);
                var block = world.BlockAccessor.GetBlock(spos) as BlockHorizontal2BMultiblock;
                if(block != null) block.RemoveSurrogateBlock(world, spos);

                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }
        }

        protected virtual void RemoveSurrogateBlock(IWorldAccessor world, BlockPos pos)
        {
            if(EntityClass != null)
            {
                world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
            }
            world.BlockAccessor.SetBlock(0, pos);
        }

        protected BlockPos GetMainBlockPosition(BlockPos pos)
        {
            if(isSurrogate) return pos.AddCopy(oppositeFace);
            return pos;
        }

        protected BlockSelection GetMainBlockSelection(BlockSelection blockSel)
        {
            if(isSurrogate)
            {
                var sel = blockSel.Clone();
                sel.Position.Add(oppositeFace);
                sel.HitPosition.Add(oppositeFace.Opposite.Normald);
                return sel;
            }
            return blockSel;
        }
    }
}