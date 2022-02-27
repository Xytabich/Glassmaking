using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructure : Block
	{
		//These values are set by the main block
		protected internal Vec3i mainOffset = null;
		protected internal bool isSurrogate = false;

		public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
		{
			if(isSurrogate)
			{
				var mainPos = GetMainBlockPosition(pos);
				if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontalStructure mainBlock)
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

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{
			if(isSurrogate)
			{
				var mainPos = GetMainBlockPosition(pos);
				if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontalStructure mainBlock)
				{
					return mainBlock.GetPlacedBlockName(world, mainPos);
				}
			}
			return base.GetPlacedBlockName(world, pos);
		}

		public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
		{
			if(isSurrogate)
			{
				var mainPos = GetMainBlockPosition(pos);
				if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontalStructure mainBlock)
				{
					return mainBlock.GetPlacedBlockInfo(world, mainPos, forPlayer);
				}
			}
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)//TODO: check claims
		{
			if(isSurrogate)
			{
				var mainPos = GetMainBlockPosition(pos);
				if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontalStructure)
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
				base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
			}
		}

		public BlockPos GetMainBlockPosition(BlockPos pos)
		{
			if(isSurrogate) return pos.AddCopy(mainOffset);
			return pos;
		}

		protected internal virtual void InitSurrogate(Vec3i mainOffset)
		{
			if(isSurrogate)
			{
				if(this.mainOffset.Equals(mainOffset)) return;

				throw new Exception("Unable to initialize surrogate with different main block coordinates");
			}
			this.mainOffset = mainOffset;
			isSurrogate = true;
		}

		protected internal virtual void RemoveSurrogateBlock(IWorldAccessor world, BlockPos pos)
		{
			if(EntityClass != null)
			{
				world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
			}
			world.BlockAccessor.SetBlock(0, pos);
		}

		protected BlockSelection GetMainBlockSelection(BlockSelection blockSel)
		{
			if(isSurrogate)
			{
				var sel = blockSel.Clone();
				sel.Position.Add(mainOffset);
				sel.HitPosition.Add(-mainOffset.X, -mainOffset.Y, -mainOffset.Z);
				return sel;
			}
			return blockSel;
		}
	}
}