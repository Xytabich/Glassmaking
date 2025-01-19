using GlassMaking.Blocks.Multiblock;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockWorkbench : BlockHorizontalStructure
	{
		public ModelTransform DefaultWorkpieceTransform = default!;

		protected override void OnStructureLoaded()
		{
			base.OnStructureLoaded();
			if(!isSurrogate && api.Side == EnumAppSide.Client)
			{
				DefaultWorkpieceTransform = Attributes["workpieceTransform"].AsObject<ModelTransform>().EnsureDefaultValues();
			}
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
		{
			blockSel = GetMainBlockSelection(blockSel);
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null) return be.GetBlockInteractionHelp(world, GetToolSelection(this, blockSel), forPlayer);
			return base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			blockSel = GetMainBlockSelection(blockSel);
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				bool result = be.OnBlockInteractStart(world, byPlayer, GetToolSelection(this, blockSel), ref handling);
				if(handling != EnumHandling.PassThrough) return result;
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			blockSel = GetMainBlockSelection(blockSel);
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				bool result = be.OnBlockInteractStep(secondsUsed, world, byPlayer, GetToolSelection(this, blockSel), ref handling);
				if(handling != EnumHandling.PassThrough) return result;
			}
			return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
		}

		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			blockSel = GetMainBlockSelection(blockSel);
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				be.OnBlockInteractStop(secondsUsed, world, byPlayer, GetToolSelection(this, blockSel), ref handling);
				if(handling != EnumHandling.PassThrough) return;
			}
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
		}

		public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
		{
			blockSel = GetMainBlockSelection(blockSel);
			var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWorkbench;
			if(be != null)
			{
				var handling = EnumHandling.PassThrough;
				bool result = be.OnBlockInteractCancel(secondsUsed, world, byPlayer, GetToolSelection(this, blockSel), ref handling);
				if(handling != EnumHandling.PassThrough) return result;
			}
			return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
		}

		public override int GetColor(ICoreClientAPI capi, BlockPos pos)
		{
			if(isSurrogate)
			{
				pos = GetMainBlockPosition(pos);
				return capi.World.BlockAccessor.GetBlock(pos).GetColor(capi, pos);
			}
			return base.GetColor(capi, pos);
		}

		public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
		{
			if(isSurrogate)
			{
				pos = GetMainBlockPosition(pos);
				return capi.World.BlockAccessor.GetBlock(pos).GetColorWithoutTint(capi, pos);
			}
			return base.GetColorWithoutTint(capi, pos);
		}

		public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
		{
			if(isSurrogate)
			{
				pos = GetMainBlockPosition(pos);
				return capi.World.BlockAccessor.GetBlock(pos).GetRandomColor(capi, pos, facing, rndIndex);
			}
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}

		public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
		{
			var blockBoxes = base.GetSelectionBoxes(blockAccessor, pos) ?? new Cuboidf[0];

			pos = GetMainBlockPosition(pos);
			var be = blockAccessor.GetBlockEntity(pos) as BlockEntityWorkbench;
			if(be != null)
			{
				var toolBoxes = be.GetToolSelectionBoxes();
				if(isSurrogate)
				{
					var boxes = new Cuboidf[blockBoxes.Length + toolBoxes.Length];
					blockBoxes.CopyTo(boxes, 0);
					for(int i = 0; i < toolBoxes.Length; i++)
					{
						boxes[blockBoxes.Length + i] = toolBoxes[i].OffsetCopy(mainOffset.X, mainOffset.Y, mainOffset.Z);
					}

					return boxes;
				}
				return blockBoxes.Append(be.GetToolSelectionBoxes());
			}

			return blockBoxes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static BlockSelection GetToolSelection(Block block, BlockSelection blockSel)
		{
			blockSel = blockSel.Clone();
			blockSel.SelectionBoxIndex -= block.SelectionBoxes == null ? 1 : block.SelectionBoxes.Length;
			return blockSel;
		}
	}
}