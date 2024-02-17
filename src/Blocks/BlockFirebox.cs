using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
	public class BlockFirebox : Block, IIgnitable
	{
		public double tempIncreasePerHour;
		public double tempDecreasePerHour;

		private WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			tempIncreasePerHour = Attributes["heatingRate"].AsDouble();
			tempDecreasePerHour = Attributes["coolingRate"].AsDouble();

			if(api.Side != EnumAppSide.Client) return;
			ICoreClientAPI capi = api as ICoreClientAPI;

			interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:blockhelp-firebox", () => {
				List<ItemStack> fuelStacklist = new List<ItemStack>();
				List<ItemStack> canIgniteStacks = new List<ItemStack>();

				foreach(CollectibleObject obj in api.World.Collectibles)
				{
					if(obj.CombustibleProps?.BurnTemperature >= 100)
					{
						List<ItemStack> stacks = obj.GetHandBookStacks(capi);
						if(stacks != null) fuelStacklist.AddRange(stacks);
					}
					if(obj is Block block && block.HasBehavior<BlockBehaviorCanIgnite>())
					{
						List<ItemStack> stacks = obj.GetHandBookStacks(capi);
						if(stacks != null) canIgniteStacks.AddRange(stacks);
					}
				}

				return new WorldInteraction[] {
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-firebox-fuel",
						HotKeyCode = null,
						MouseButton = EnumMouseButton.Right,
						Itemstacks = fuelStacklist.ToArray(),
						GetMatchingStacks = GetMatchingFuel
					},
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-firebox-fuel",
						HotKeyCode = "sprint",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = fuelStacklist.ConvertAll(s => { s = s.Clone(); s.StackSize = 5; return s; }).ToArray(),
						GetMatchingStacks = GetMatchingFuel
					},
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-firebox-ignite",
						HotKeyCode = "sneak",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = canIgniteStacks.ToArray(),
						GetMatchingStacks = GetMatchingIgnitor
					}
				};
			});
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack itemstack = slot.Itemstack;
			if(itemstack != null)
			{
				BlockEntityFirebox be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFirebox;
				if(be != null)
				{
					if(itemstack.Class == EnumItemClass.Block && itemstack.Block is IHeaterPlaceableBlock block)
					{
						if(block.TryPlaceBlock(world, byPlayer, new BlockSelection { Position = blockSel.Position.UpCopy(), Face = BlockFacing.UP }, itemstack, Variant["side"]))
						{
							world.PlaySoundAt(itemstack.Block.GetSounds(world.BlockAccessor, blockSel.Position.UpCopy(), itemstack)?.Place,
								blockSel.Position.X + 0.5, blockSel.Position.Y + 1, blockSel.Position.Z + 0.5, byPlayer, true, 16f);
							if(byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
							{
								byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
							}
							be.SetReceiver(world.BlockAccessor.GetBlockEntity(blockSel.Position.UpCopy()) as ITimeBasedHeatReceiver);
							return true;
						}
					}
					if(be.TryAdd(byPlayer, slot, byPlayer.Entity.Controls.Sprint ? 5 : 1))
					{
						if(world.Side == EnumAppSide.Client)
						{
							(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
						}
						return true;
					}
				}
			}
			return false;
		}

		public virtual EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
		{
			if(!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox).CanIgnite())
			{
				return EnumIgniteState.NotIgnitablePreventDefault;
			}
			if(!(secondsIgniting > 4f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}

		public virtual void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
		{
			handling = EnumHandling.PreventDefault;
			(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox)?.TryIgnite();
		}

		public virtual EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
		{
			if(!(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox).CanIgnite())
			{
				return EnumIgniteState.NotIgnitablePreventDefault;
			}
			if(!(secondsIgniting > 4f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}

		public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			var upPos = pos.UpCopy();
			var block = world.BlockAccessor.GetBlock(upPos);
			if(block is IHeaterPlaceableBlock) block.OnBlockBroken(world, upPos, byPlayer, dropQuantityMultiplier);
			base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		}

		public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
		{
			var upPos = pos.UpCopy();
			var handle = BulkAccessUtil.SetReadFromStagedByDefault(world.BulkBlockAccessor, true);
			var block = world.BulkBlockAccessor.GetBlock(upPos);
			handle.RollbackValue();

			if(block is IHeaterPlaceableBlock) block.OnBlockExploded(world, pos, explosionCenter, blastType);

			base.OnBlockExploded(world, pos, explosionCenter, blastType);
		}

		public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			var items = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if(items == null) items = new ItemStack[0];
			var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox;
			if(be != null) items = items.Append(be.GetDropItems() ?? new ItemStack[0]);
			return items;
		}

		public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
		{
			base.OnNeighbourBlockChange(world, pos, neibpos);
			if(neibpos == pos.UpCopy())
			{
				var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirebox;
				if(be != null)
				{
					be.SetReceiver(world.BlockAccessor.GetBlockEntity(neibpos) as ITimeBasedHeatReceiver);
				}
			}
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}

		private ItemStack[] GetMatchingFuel(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
		{
			if(wi.Itemstacks.Length == 0) return null;
			var be = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BlockEntityFirebox;
			if(be == null) return null;
			be.GetFuelStackState(out var canAddAmount, out var stack);
			if(stack == null) return wi.Itemstacks;
			if(canAddAmount < wi.Itemstacks[0].StackSize) return null;
			stack = stack.GetEmptyClone();
			stack.StackSize = wi.Itemstacks[0].StackSize;
			return new ItemStack[] { stack };
		}

		private ItemStack[] GetMatchingIgnitor(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
		{
			if(wi.Itemstacks.Length == 0) return null;
			var be = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BlockEntityFirebox;
			if(be == null) return null;
			if(be.IsBurning()) return null;
			return wi.Itemstacks;
		}
	}
}