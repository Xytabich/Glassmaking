using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructure : Block
	{
		//These values are provided by the main block
		public Vec3i mainOffset = default!;
		public bool isSurrogate = false;

		private int loadStep = 0;

		protected Vec3i structureOffset = default!;
		protected internal Block?[,,] structure = default!;

		protected JsonItemStack? handbookStack = null;

		public sealed override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			if(Attributes != null && Attributes.KeyExists("structure"))
			{
				mainOffset = Vec3i.Zero;
				var codes = Attributes["structure"].AsObject<Structure>(null!, Code.Domain).GetRotated();
				int sx = codes.GetLength(0), sy = codes.GetLength(1), sz = codes.GetLength(2);
				structure = new Block?[sx, sy, sz];
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(codes[x, y, z] != null)
							{
								if(string.IsNullOrWhiteSpace(codes[x, y, z]!.Path))
								{
									codes[x, y, z] = null;
								}
								else
								{
									structure[x, y, z] = api.World.GetBlock(codes[x, y, z]);
									if(structure[x, y, z]!.Id == Id)
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
							if(structure[x, y, z] is BlockHorizontalStructure sblock)
							{
								if(sblock.isSurrogate)
								{
									if(sblock.mainOffset.Equals(mainOffset)) continue;

									throw new Exception(string.Join("Unable to initialize surrogate {0} with different main block coordinates", sblock.Code));
								}
								sblock.InitStructurePart(api, sblock.Id != Id, new Vec3i(-(x + structureOffset.X), -(y + structureOffset.Y), -(z + structureOffset.Z)));
							}
						}
					}
				}
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] is BlockHorizontalStructure sblock)
							{
								sblock.OnStepLoaded();
							}
						}
					}
				}
			}

			OnStepLoaded();
		}

		public override List<ItemStack>? GetHandBookStacks(ICoreClientAPI capi)
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

		public override ItemStack? OnPickBlock(IWorldAccessor world, BlockPos pos)
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

		public override ItemStack[]? GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			if(isSurrogate) return null;
			var items = new List<ItemStack>(base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier));
			var offset = new Vec3i();
			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] == null || structure[x, y, z]!.Id == Id) continue;

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
								var drops = sblock.GetSurrogateDrops(world, spos, byPlayer, dropQuantityMultiplier);
								if((drops?.Length ?? 0) != 0) items.AddRange(drops!);
							}
						}
					}
				}
			}
			return items.ToArray();
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
			if(base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				if(isSurrogate) return true;

				var sel = blockSel.Clone();
				int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
				for(int x = 0; x < sx; x++)
				{
					for(int y = 0; y < sy; y++)
					{
						for(int z = 0; z < sz; z++)
						{
							if(structure[x, y, z] == null || structure[x, y, z]!.Id == Id) continue;

							sel.Position.SetAll(blockSel.Position);
							sel.Position.Add(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
							if(!structure[x, y, z]!.CanPlaceBlock(world, byPlayer, sel, ref failureCode))
							{
								return false;
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
		{
			if(isSurrogate) return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

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
							if(structure[x, y, z] == null || structure[x, y, z]!.Id == Id) continue;

							sel.Position.SetAll(blockSel.Position);
							sel.Position.Add(x + structureOffset.X, y + structureOffset.Y, z + structureOffset.Z);
							var pos = sel.Position.Copy();
							var block = world.BlockAccessor.GetBlock(sel.Position);
							structure[x, y, z]!.DoPlaceBlock(world, byPlayer, sel, byItemStack);
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
			if(isSurrogate)
			{
				var mainPos = GetMainBlockPosition(pos);
				if(world.BlockAccessor.GetBlock(mainPos) is BlockHorizontalStructure)
				{
					world.BlockAccessor.BreakBlock(mainPos, byPlayer, dropQuantityMultiplier);
				}
				else
				{
					RemoveSurrogateBlock(world.BlockAccessor, pos);
				}
			}
			else
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
							if(structure[x, y, z] == null || structure[x, y, z]!.Id == Id) continue;

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
		}

		public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
		{
			if(isSurrogate)
			{
				var mainPos = GetMainBlockPosition(pos);
				var handle = BulkAccessUtil.SetReadFromStagedByDefault(world.BulkBlockAccessor, true);
				if(world.BulkBlockAccessor.GetBlock(mainPos) is BlockHorizontalStructure mainBlock)
				{
					handle.RollbackValue();
					mainBlock.OnBlockExploded(world, mainPos, explosionCenter, blastType);
				}
				else
				{
					handle.RollbackValue();
					RemoveSurrogateBlock(world.BulkBlockAccessor, pos);
				}
			}
			else
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
							if(structure[x, y, z] == null || structure[x, y, z]!.Id == Id) continue;

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

										if((drops?.Length ?? 0) != 0)
										{
											for(int i = 0; i < drops!.Length; i++)
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
		}

		public BlockPos GetMainBlockPosition(BlockPos pos)
		{
			if(isSurrogate) return pos.AddCopy(mainOffset);
			return pos;
		}

		public Block? GetStructureBlock(Vec3i offset)
		{
			if(isSurrogate) throw new Exception("GetStructureBlock called on surrogate");
			int x = offset.X - structureOffset.X;
			if(x < 0 || x >= structure.GetLength(0)) return null;
			int y = offset.Y - structureOffset.Y;
			if(y < 0 || y >= structure.GetLength(1)) return null;
			int z = offset.Z - structureOffset.Z;
			if(z < 0 || z >= structure.GetLength(2)) return null;
			return structure[x, y, z];
		}

		internal void OnStepLoaded()
		{
			loadStep++;
			if(loadStep == 2) OnStructureLoaded();
		}

		protected virtual void InitStructurePart(ICoreAPI api, bool isSurrogate, Vec3i mainOffset, bool isVariant = false)
		{
			this.isSurrogate = isSurrogate;
			this.mainOffset = mainOffset;
			if(isSurrogate && !isVariant)
			{
				if(Attributes != null && Attributes.KeyExists("structureSurrogate") && Attributes["structureSurrogate"].KeyExists("subVariants"))
				{
					var subVariants = Attributes["structureSurrogate"]["subVariants"].AsObject<Dictionary<string, string[]>>();
					if(subVariants != null && subVariants.Count > 0)
					{
						foreach(var pair in subVariants)
						{
							if(!string.IsNullOrEmpty(pair.Key) && pair.Value != null && pair.Value.Length > 0)
							{
								foreach(var v in pair.Value)
								{
									var block = api.World.GetBlock(CodeWithVariant(pair.Key, v));
									if(block is BlockHorizontalStructure part)
									{
										part.InitStructurePart(api, true, mainOffset, true);
									}
								}
							}
						}
					}
				}
			}
			if(isSurrogate)
			{
				ShapeInventory = DefaultCubeShape;
			}
		}

		protected virtual void OnStructureLoaded()
		{
			if(api.Side == EnumAppSide.Client)
			{
				handbookStack = Attributes?["handbookStack"].AsObject<JsonItemStack?>(null, Code.Domain);
				if(handbookStack != null)
				{
					if(!handbookStack.Resolve(api.World, "structure handbook stack"))
					{
						handbookStack = null;
					}
				}
			}
		}

		protected virtual ItemStack[] GetSurrogateDrops(IWorldAccessor world, BlockPos pos, IPlayer? byPlayer, float dropQuantityMultiplier = 1)
		{
			return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		}

		protected virtual void RemoveSurrogateBlock(IBlockAccessor blockAccessor, BlockPos pos)
		{
			if(EntityClass != null)
			{
				blockAccessor.GetBlockEntity(pos)?.OnBlockBroken();
			}
			blockAccessor.SetBlock(0, pos);
			blockAccessor.TriggerNeighbourBlockUpdate(pos.Copy());
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
			public AssetLocation?[,,] codes = default!;
			public int rotateY = default;

			public AssetLocation?[,,] GetRotated()
			{
				switch(rotateY)
				{
					case 90:
						{
							int sx = codes.GetLength(0), sy = codes.GetLength(1), sz = codes.GetLength(2);
							var arr = new AssetLocation?[sz, sy, sx];
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
							var arr = new AssetLocation?[sx, sy, sz];
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
							var arr = new AssetLocation?[sz, sy, sx];
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