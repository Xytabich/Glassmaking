using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class HeatedBlockBase : Block, IHeaterPlaceableBlock
	{
		public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack itemstack, string side)
		{
			if(world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(this))
			{
				return world.GetBlock(CodeWithVariant("side", side)).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			}
			return false;
		}

		public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, string ignitedByPlayerUid)
		{
			var handle = BulkAccessUtil.SetReadFromStagedByDefault(world.BulkBlockAccessor, true);
			var id = world.BulkBlockAccessor.GetBlockId(pos);
			handle.RollbackValue();
			// Since the firebox can destroy this block during the explosion, it is necessary to first check if it was destroyed
			if(id != Id) return;

			base.OnBlockExploded(world, pos, explosionCenter, blastType, ignitedByPlayerUid);
		}
	}
}