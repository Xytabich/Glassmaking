using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructure : Block
	{
		//These values are set by the main block
		protected internal Vec3i mainOffset = null;
		protected internal bool isSurrogate = false;

		protected JsonItemStack handbookStack = null;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			if(api.Side == EnumAppSide.Client)
			{
				handbookStack = Attributes?["handbookStack"].AsObject<JsonItemStack>(null, Code.Domain);
				if(handbookStack != null)
				{
					if(!handbookStack.Resolve(api.World, "structure handbook stack"))
					{
						handbookStack = null;
					}
				}
			}
		}

		public override List<ItemStack> GetHandBookStacks(ICoreClientAPI capi)
		{
			if(isSurrogate)
			{
				if(handbookStack != null)
				{
					return new List<ItemStack>() { handbookStack.ResolvedItemstack };
				}
				return null;
			}
			return base.GetHandBookStacks(capi);
		}

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

		protected internal virtual ItemStack[] GetSurrogateDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		}

		protected internal virtual void RemoveSurrogateBlock(IWorldAccessor world, BlockPos pos)
		{
			if(EntityClass != null)
			{
				world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
			}
			world.BlockAccessor.SetBlock(0, pos);
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos.Copy());
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