using Newtonsoft.Json;
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

		private bool isLoaded = false;

		private WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			isLoaded = true;
			replacement = Attributes["replacement"].AsObject<ReplacementInfo>();
			InitReplacement();
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			var itemStack = byPlayer.Entity.RightHandItemSlot.Itemstack;
			if(itemStack != null)
			{
				var requirement = (replacement.requirement ?? replacement.block);
				if(requirement.Matches(world, itemStack) && itemStack.StackSize >= requirement.ResolvedItemstack.StackSize)
				{
					var item = byPlayer.Entity.RightHandItemSlot.TakeOut(requirement.ResolvedItemstack.StackSize);
					RemoveSurrogateBlock(world, blockSel.Position);

					replacement.block.ResolvedItemstack.Block.DoPlaceBlock(world, byPlayer,
						blockSel, replacement.block == requirement ? item : replacement.block.ResolvedItemstack);

					if(world.BlockAccessor.GetBlock(GetMainBlockPosition(blockSel.Position)) is IStructurePlanMainBlock mainBlock)
					{
						mainBlock.OnSurrogateReplaced(world, byPlayer, blockSel);
					}
					return true;
				}
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}

		protected internal override void InitSurrogate(Vec3i mainOffset)
		{
			base.InitSurrogate(mainOffset);
			InitReplacement();
		}

		private void InitReplacement()
		{
			if(isLoaded && isSurrogate)
			{
				if(replacement.block.Resolve(api.World, "structure plan block"))
				{
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
						structure.InitSurrogate(mainOffset);
					}
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

		[JsonObject]
		protected class ReplacementInfo
		{
			[JsonProperty(Required = Required.Always)]
			public JsonItemStack block = null;
			public JsonItemStack requirement = null;
		}
	}
}