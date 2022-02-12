using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockWorkbench : BlockHorizontal2BMultiblock
	{
		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				bool result = be.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
				if(handling != EnumHandling.PassThrough) return result;
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				bool result = be.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel, ref handling);
				if(handling != EnumHandling.PassThrough) return result;
			}
			return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
		}

		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				be.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
				if(handling != EnumHandling.PassThrough) return;
			}
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
		}

		public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
		{
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				bool result = be.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, ref handling);
				if(handling != EnumHandling.PassThrough) return result;
			}
			return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
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