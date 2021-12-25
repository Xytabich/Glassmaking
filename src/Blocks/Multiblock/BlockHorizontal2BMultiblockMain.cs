using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockHorizontal2BMultiblockMain : Block
    {
        protected BlockFacing surrogateSideFace;

        public override void OnLoaded(ICoreAPI api)
        {
            string side;
            if(!Variant.TryGetValue("horizontalorientation", out side))
            {
                side = Variant["side"];
            }
            var face = BlockFacing.FromCode(side);
            surrogateSideFace = BlockFacing.HORIZONTALS_ANGLEORDER[(face.HorizontalAngleIndex + 1) % 4];
        }

        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            if(!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }
            var surrogate = new AssetLocation(Attributes["surrogate"].AsString());
            blockSel = blockSel.Clone();
            blockSel.Position = blockSel.Position.AddCopy(surrogateSideFace);
            return world.GetBlock(surrogate).CanPlaceBlock(world, byPlayer, blockSel, ref failureCode);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            if(base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
            {
                blockSel = blockSel.Clone();
                blockSel.Position = blockSel.Position.AddCopy(surrogateSideFace);
                var surrogate = new AssetLocation(Attributes["surrogate"].AsString());
                world.GetBlock(surrogate).DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
                return true;
            }
            return false;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var spos = pos.AddCopy(surrogateSideFace);
            var block = world.BlockAccessor.GetBlock(spos) as BlockHorizontal2BMultiblockSurrogate;
            if(block != null) block.RemoveBlock(world, spos);

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}