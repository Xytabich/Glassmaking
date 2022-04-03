﻿using Newtonsoft.Json;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructurePlan : BlockHorizontalStructure
	{
		protected ReplacementInfo replacement;

		private WorldInteraction[] interactions;

		protected internal override void OnStructureLoaded(bool isSurrogate, Vec3i mainOffset)
		{
			base.OnStructureLoaded(isSurrogate, mainOffset);

			if(isSurrogate)
			{
				replacement = Attributes["replacement"].AsObject<ReplacementInfo>(null, Code.Domain);
				replacement.Resolve(api.World);
				if(replacement.block.Type != EnumItemClass.Block)
				{
					throw new Exception("The replacement must be a block");
				}
				if(replacement.requirement != null)
				{
					replacement.requirement.Resolve(api.World, "structure plan requirement");
				}
				if(replacement.block.ResolvedItemstack.Block is BlockHorizontalStructure structure)
				{
					structure.OnStructureLoaded(isSurrogate, mainOffset);
				}
				if(api.Side == EnumAppSide.Client)
				{
					interactions = new WorldInteraction[] {
						new WorldInteraction()
						{
							ActionLangCode = "glassmaking:blockhelp-plan-put",
							HotKeyCode = null,
							MouseButton = EnumMouseButton.Right,
							Itemstacks = new ItemStack[] { (replacement.requirement ?? replacement.block).ResolvedItemstack }
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
				var requirement = (replacement.requirement ?? replacement.block);
				if(requirement.Matches(world, itemStack) && itemStack.StackSize >= requirement.ResolvedItemstack.StackSize)
				{
					var item = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(requirement.ResolvedItemstack.StackSize);
					RemoveSurrogateBlock(world.BlockAccessor, blockSel.Position);

					var block = replacement.block.ResolvedItemstack.Block;
					var stack = replacement.block == requirement ? item : replacement.block.ResolvedItemstack;
					world.PlaySoundAt(block.GetSounds(world.BlockAccessor, blockSel.Position, stack)?.Place,
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
		protected class ReplacementInfo
		{
			[JsonProperty(Required = Required.Always)]
			public JsonItemStack block;
			public JsonItemStack requirement = null;

			public void Resolve(IWorldAccessor world)
			{
				block.Resolve(world, "structure plan");
				requirement?.Resolve(world, "structure plan");
			}
		}
	}
}