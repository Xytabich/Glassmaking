using Newtonsoft.Json;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructurePlan : BlockHorizontalStructure
	{
		protected internal ReplacementInfo replacement = default!;

		private WorldInteraction[] interactions = default!;

		protected override void OnStructureLoaded()
		{
			base.OnStructureLoaded();

			if(isSurrogate)
			{
				replacement = Attributes["replacement"].AsObject<ReplacementInfo>(null!, Code.Domain);
				replacement.Resolve(api.World);
				if(replacement.Block.Type != EnumItemClass.Block)
				{
					throw new Exception("The replacement must be a block");
				}
				if(replacement.Block.ResolvedItemstack.Block is BlockHorizontalStructure structure)
				{
					structure.isSurrogate = isSurrogate;
					structure.mainOffset = mainOffset;
					structure.OnStepLoaded();
				}
				if(api.Side == EnumAppSide.Client)
				{
					interactions = new WorldInteraction[] {
						new WorldInteraction()
						{
							ActionLangCode = "glassmaking:blockhelp-plan-put",
							HotKeyCode = null,
							MouseButton = EnumMouseButton.Right,
							Itemstacks = new ItemStack[] { (replacement.Requirement ?? replacement.Block).ResolvedItemstack }
						}
					};
				}
			}
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			var itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			if(itemStack != null)
			{
				var requirement = replacement.Requirement ?? replacement.Block;
				if(requirement.Matches(world, itemStack) && itemStack.StackSize >= requirement.ResolvedItemstack.StackSize)
				{
					var item = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(requirement.ResolvedItemstack.StackSize);
					RemoveSurrogateBlock(world.BlockAccessor, blockSel.Position);

					var block = replacement.Block.ResolvedItemstack.Block;
					var stack = replacement.Block == requirement ? item : replacement.Block.ResolvedItemstack;
					world.PlaySoundAt(block.GetSounds(world.BlockAccessor, blockSel, stack)?.Place,
						blockSel.Position.X + 0.5, blockSel.Position.Y + 0.5, blockSel.Position.Z + 0.5, byPlayer, true, 16f);

					block.DoPlaceBlock(world, byPlayer, blockSel, stack);
					world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position.Copy());

					if(world.BlockAccessor.GetBlockEntity(GetMainBlockPosition(blockSel.Position)) is IStructurePlanMainBlock mainBlockEntity)
					{
						mainBlockEntity.OnSurrogateReplaced(world, byPlayer, blockSel, this, block);
					}
					else if(world.BlockAccessor.GetBlock(GetMainBlockPosition(blockSel.Position)) is IStructurePlanMainBlock mainBlock)
					{
						mainBlock.OnSurrogateReplaced(world, byPlayer, blockSel, this, block);
					}
					return true;
				}
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}

		[JsonObject]
		protected internal class ReplacementInfo
		{
			[JsonProperty(Required = Required.Always)]
			public JsonItemStack Block = default!;
			public JsonItemStack? Requirement = null;

			public void Resolve(IWorldAccessor world)
			{
				Block.Resolve(world, "structure plan");
				Requirement?.Resolve(world, "structure plan requirement");
			}
		}
	}
}