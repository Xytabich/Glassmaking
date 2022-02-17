using Newtonsoft.Json;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockHorizontalStructure : Block
	{
		protected Vec3i structureOffset = null;
		protected AssetLocation[,,] structure;

		//These values are set by the main block
		protected Vec3i mainOffset;
		protected bool isSurrogate = false;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if(Attributes != null && Attributes.KeyExists("structure"))
			{
				mainOffset = Vec3i.Zero;
				this.structure = Attributes["structure"].AsObject<Structure>(null, Code.Domain).GetRotated();
				int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] != null)
							{
								if(string.IsNullOrWhiteSpace(structure[x, y, z].Path))
								{
									structure[x, y, z] = null;
								}
								else if(Code.Equals(structure[x, y, z]))
								{
									if(structureOffset != null)
									{
										throw new Exception("Structure must have only one main block");
									}
									structureOffset = new Vec3i(-x, -y, -z);
								}
							}
						}
					}
				}
				if(structureOffset == null)
				{
					throw new Exception("The structure must include the main block");
				}
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] != null && !Code.Equals(structure[x, y, z]))
							{
								if(api.World.GetBlock(structure[x, y, z]) is BlockHorizontalStructure sblock)
								{
									sblock.InitSurrogate(new Vec3i(-(x + structureOffset.X), -(y + structureOffset.Y), -(z + structureOffset.Z)));
								}
							}
						}
					}
				}
			}
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

		public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
		{
			if(!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				return false;
			}
			if(isSurrogate) return true;

			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] == null || Code.Equals(structure[x, y, z])) continue;

						var sel = blockSel.Clone();
						sel.Position = blockSel.Position.AddCopy(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
						if(!world.GetBlock(structure[x, y, z]).CanPlaceBlock(world, byPlayer, sel, ref failureCode))
						{
							return false;
						}
					}
				}
			}
			return true;
		}

		public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
		{
			if(base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
			{
				if(isSurrogate) return true;

				int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] == null || Code.Equals(structure[x, y, z])) continue;

							var sel = blockSel.Clone();
							sel.Position = blockSel.Position.AddCopy(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
							world.GetBlock(structure[x, y, z]).DoPlaceBlock(world, byPlayer, sel, byItemStack);
						}
					}
				}

				return true;
			}
			return false;
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
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
				var offset = new Vec3i();
				int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] == null || Code.Equals(structure[x, y, z])) continue;

							offset.Set(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
							var spos = pos.AddCopy(offset);
							var block = world.BlockAccessor.GetBlock(spos);
							if(block is BlockHorizontalStructure sblock && sblock.isSurrogate)
							{
								offset.X = -offset.X;
								offset.Y = -offset.Y;
								offset.Z = -offset.Z;
								if(sblock.mainOffset.Equals(offset))
								{
									sblock.RemoveSurrogateBlock(world, spos);
								}
							}
						}
					}
				}

				base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
			}
		}

		protected internal virtual void InitSurrogate(Vec3i mainOffset)
		{
			if(isSurrogate)
			{
				if(this.mainOffset.Equals(mainOffset)) return;

				throw new Exception("Unable to initialize structure with different main block coordinates");
			}
			this.mainOffset = mainOffset;
			isSurrogate = true;
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
			if(isSurrogate) return pos.AddCopy(mainOffset);
			return pos;
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

		[JsonObject]
		private class Structure
		{
			public AssetLocation[,,] codes;
			public int rotateY;

			public AssetLocation[,,] GetRotated()
			{
				switch(rotateY)
				{
					case 90:
						{
							int sx = codes.GetLength(0), sy = codes.GetLength(1), sz = codes.GetLength(2);
							var arr = new AssetLocation[sz, sy, sx];
							for(int x = 0; x < sx; x++)
							{
								for(int y = 0; y < sy; y++)
								{
									for(int z = 0; z < sz; z++)
									{
										arr[z, y, x] = codes[x, y, z];
									}
								}
							}
							return arr;
						}
					case 180:
						{
							int sx = codes.GetLength(0), sy = codes.GetLength(1), sz = codes.GetLength(2);
							var arr = new AssetLocation[sx, sy, sz];
							sx--;
							sy--;
							sz--;
							for(int x = 0; x <= sx; x++)
							{
								for(int y = 0; y <= sy; y++)
								{
									for(int z = 0; z <= sz; z++)
									{
										arr[x, y, z] = codes[sx - x, y, sz - z];
									}
								}
							}
							return arr;
						}
					case 270:
						{
							int sx = codes.GetLength(0), sy = codes.GetLength(1), sz = codes.GetLength(2);
							var arr = new AssetLocation[sz, sy, sx];
							sx--;
							sy--;
							sz--;
							for(int x = 0; x <= sx; x++)
							{
								for(int y = 0; y <= sy; y++)
								{
									for(int z = 0; z <= sz; z++)
									{
										arr[z, y, x] = codes[sx - x, y, sz - z];
									}
								}
							}
							return arr;
						}
					default: return codes;
				}
			}
		}
	}
}