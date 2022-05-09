using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockBehaviorPlaceTable : BlockBehavior
	{
		private string variantCode = "horizontalorientation";

		public BlockBehaviorPlaceTable(Block block)
			: base(block)
		{
			if(!block.Variant.ContainsKey("horizontalorientation"))
			{
				variantCode = "side";
			}
		}

		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
		{
			handling = EnumHandling.PreventDefault;
			BlockPos blockPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double x = (byPlayer.Entity.Pos.X + byPlayer.Entity.LocalEyePos.X) - (blockPos.X + blockSel.HitPosition.X);
			double z = (byPlayer.Entity.Pos.Z + byPlayer.Entity.LocalEyePos.Z) - (blockPos.Z + blockSel.HitPosition.Z);
			var face = BlockFacing.HorizontalFromAngle((float)(Math.Atan2(x, z) + Math.PI / 2));
			AssetLocation assetLocation = base.block.CodeWithVariant(variantCode, face.Code);
			Block block = world.BlockAccessor.GetBlock(assetLocation);
			if(block == null)
			{
				throw new NullReferenceException("Unable to to find a rotated block with code " + assetLocation.ToString() + ", you're maybe missing the side variant group of have a dash in your block code");
			}
			var sideFace = BlockFacing.HORIZONTALS_ANGLEORDER[(face.HorizontalAngleIndex + 3) % 4];
			if(sideFace.Normald.Dot(new Vec3d(blockSel.HitPosition.X - 0.5, 0, blockSel.HitPosition.Z - 0.5)) >= 0)
			{
				blockSel = new BlockSelection()
				{
					Face = blockSel.Face,
					Position = blockSel.Position.AddCopy(sideFace.Normali),
					HitPosition = blockSel.HitPosition
				};
			}
			if(block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				block.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
				return true;
			}
			return false;
		}
	}
}