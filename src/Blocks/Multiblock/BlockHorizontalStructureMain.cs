using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructureMain : BlockHorizontalStructure
	{
		protected Vec3i structureOffset = null;
		protected Block[,,] structure;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			mainOffset = Vec3i.Zero;
			var codes = Attributes["structure"].AsObject<Structure>(null, Code.Domain).GetRotated();
			int sx = codes.GetLength(0), sy = codes.GetLength(1), sz = codes.GetLength(2);
			structure = new Block[sx, sy, sz];
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(codes[x, y, z] != null)
						{
							if(string.IsNullOrWhiteSpace(codes[x, y, z].Path))
							{
								codes[x, y, z] = null;
							}
							else
							{
								structure[x, y, z] = api.World.GetBlock(codes[x, y, z]);
								if(structure[x, y, z].Id == Id)
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
			}
			if(structureOffset == null)
			{
				throw new Exception(string.Format("The structure {0} must include the main block", Code));
			}
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] != null && structure[x, y, z].Id != Id)
						{
							if(structure[x, y, z] is BlockHorizontalStructure sblock)
							{
								sblock.InitSurrogate(new Vec3i(-(x + structureOffset.X), -(y + structureOffset.Y), -(z + structureOffset.Z)));
							}
						}
					}
				}
			}
		}

		public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
		{
			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] == null || structure[x, y, z].Id == Id) continue;

						var sel = blockSel.Clone();
						sel.Position = blockSel.Position.AddCopy(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
						if(!structure[x, y, z].CanPlaceBlock(world, byPlayer, sel, ref failureCode))
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
				var sel = blockSel.Clone();
				int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] == null || structure[x, y, z].Id == Id) continue;

							sel.Position.Set(blockSel.Position);
							sel.Position.Add(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
							var pos = sel.Position.Copy();
							structure[x, y, z].DoPlaceBlock(world, byPlayer, sel, byItemStack);
							world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
						}
					}
				}

				return true;
			}
			return false;
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)//TODO: check claims
		{
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);

			//TODO: use world.BulkBlockAccess instead of world.BlockAccessor
			var offset = new Vec3i();
			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] == null || structure[x, y, z].Id == Id) continue;

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
								sblock.RemoveSurrogateBlock(world.BlockAccessor, spos);
							}
						}
					}
				}
			}

			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos.Copy());
		}

		public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
		{
			EnumHandling handling = EnumHandling.PassThrough;
			BlockBehavior[] blockBehaviors = BlockBehaviors;
			for(int i = 0; i < blockBehaviors.Length; i++)
			{
				blockBehaviors[i].OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
				if(handling == EnumHandling.PreventSubsequent)
				{
					break;
				}
			}
			if(handling == EnumHandling.PreventDefault)
			{
				return;
			}

			var handle = BulkAccessUtil.SetReadFromStagedByDefault(world.BulkBlockAccessor, true);

			var offset = new Vec3i();
			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] == null || structure[x, y, z].Id == Id) continue;

						offset.Set(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
						var spos = pos.AddCopy(offset);
						var block = world.BulkBlockAccessor.GetBlock(spos);
						if(block is BlockHorizontalStructure sblock && sblock.isSurrogate)
						{
							offset.X = -offset.X;
							offset.Y = -offset.Y;
							offset.Z = -offset.Z;
							if(sblock.mainOffset.Equals(offset))
							{
								double dropChancce = sblock.ExplosionDropChance(world, spos, blastType);

								if(world.Rand.NextDouble() < dropChancce)
								{
									ItemStack[] drops = sblock.GetSurrogateDrops(world, spos, null);

									if(drops != null)
									{
										for(int i = 0; i < drops.Length; i++)
										{
											if(SplitDropStacks)
											{
												for(int k = 0; k < drops[i].StackSize; k++)
												{
													ItemStack stack = drops[i].Clone();
													stack.StackSize = 1;
													world.SpawnItemEntity(stack, new Vec3d(spos.X + 0.5, spos.Y + 0.5, spos.Z + 0.5), null);
												}
											}
											else
											{
												world.SpawnItemEntity(drops[i].Clone(), new Vec3d(spos.X + 0.5, spos.Y + 0.5, spos.Z + 0.5), null);
											}
										}
									}
								}

								sblock.RemoveSurrogateBlock(world.BulkBlockAccessor, spos);
							}
						}
					}
				}
			}

			world.BulkBlockAccessor.TriggerNeighbourBlockUpdate(pos.Copy());

			{
				// The explosion code uses the bulk block accessor for greater performance
				world.BulkBlockAccessor.SetBlock(0, pos);

				double dropChancce = ExplosionDropChance(world, pos, blastType);

				if(world.Rand.NextDouble() < dropChancce)
				{
					// Dropping only this block
					ItemStack[] drops = base.GetDrops(world, pos, null);

					if(drops != null)
					{
						for(int i = 0; i < drops.Length; i++)
						{
							if(SplitDropStacks)
							{
								for(int k = 0; k < drops[i].StackSize; k++)
								{
									ItemStack stack = drops[i].Clone();
									stack.StackSize = 1;
									world.SpawnItemEntity(stack, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
								}
							}
							else
							{
								world.SpawnItemEntity(drops[i].Clone(), new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
							}
						}
					}
				}

				if(EntityClass != null)
				{
					BlockEntity entity = world.BulkBlockAccessor.GetBlockEntity(pos);
					if(entity != null)
					{
						entity.OnBlockBroken();
					}
				}
			}

			handle.RollbackValue();
		}

		public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			var items = new List<ItemStack>(base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier));
			var offset = new Vec3i();
			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] == null || structure[x, y, z].Id == Id) continue;

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
								var drops = sblock.GetSurrogateDrops(world, pos, byPlayer, dropQuantityMultiplier);
								if(drops != null) items.AddRange(drops);
							}
						}
					}
				}
			}
			return items.ToArray();
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
					default: return codes;
				}
			}
		}
	}
}