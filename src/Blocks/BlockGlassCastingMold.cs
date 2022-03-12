using GlassMaking.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockGlassCastingMold : Block, IGlassCastingMold
	{
		public CastingMoldRecipe[] Recipes = null;

		private WorldInteraction[] interactions;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			var recipe = Attributes?["glassmaking:castingmold"].AsObject<CastingMoldRecipe>(null, Code.Domain);
			if(recipe != null && recipe.Enabled)
			{
				var world = api.World;
				var nameToCodeMapping = recipe.GetNameToCodeMapping(world);
				List<CastingMoldRecipe> recipes = new List<CastingMoldRecipe>();
				if(nameToCodeMapping.Count > 0)
				{
					string[] variants = nameToCodeMapping["type"];
					if(variants.Length > 0)
					{
						recipes.Capacity = variants.Length;
						for(int i = 0; i < variants.Length; i++)
						{
							var rec = recipe.Clone();
							rec.Recipe.Code = rec.Recipe.Code.CopyWithPath(rec.Recipe.Code.Path.Replace("*", variants[i]));
							rec.Output.Code = rec.Output.Code.CopyWithPath(rec.Output.Code.Path.Replace("*", variants[i]));
							recipes.Add(rec);
						}
					}
					else
					{
						api.World.Logger.Warning("{0} mold make uses of wildcards, but no blocks or item matching those wildcards were found.", Code);
					}
				}
				else
				{
					recipes.Add(recipe);
				}

				string source = Code.ToString();
				for(int i = recipes.Count - 1; i >= 0; i--)
				{
					if(!recipes[i].Resolve(world, source))
					{
						recipes.RemoveAt(i);
					}
				}
				Recipes = recipes.ToArray();
			}
			else
			{
				Recipes = new CastingMoldRecipe[0];
			}

			if(api.Side != EnumAppSide.Client) return;
			interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:toolmoldBlockInteractions", () => {
				List<ItemStack> smeltedContainerStacks = new List<ItemStack>();

				foreach(CollectibleObject obj in api.World.Items)
				{
					if(obj is ItemGlassLadle)
					{
						smeltedContainerStacks.Add(new ItemStack(obj));
					}
				}

				return new WorldInteraction[] {
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-castingmold-pour",
						HotKeyCode = "sneak",
						MouseButton = EnumMouseButton.Right,
						Itemstacks = smeltedContainerStacks.ToArray(),
						GetMatchingStacks = (wi, bs, es) =>
						{
							var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGlassCastingMold;
							return (be != null && !be.IsFull) ? wi.Itemstacks : null;
						}
					},
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-castingmold-takeitem",
						HotKeyCode = null,
						MouseButton = EnumMouseButton.Right,
						ShouldApply = (wi, bs, es) =>
						{
							var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGlassCastingMold;
							return be != null && be.IsFull && be.IsHardened;
						}
					},
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-castingmold-pickup",
						HotKeyCode = null,
						RequireFreeHand = true,
						MouseButton = EnumMouseButton.Right,
						ShouldApply = (wi, bs, es) =>
						{
							var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGlassCastingMold;
							return be != null && be.IsEmpty;
						}
					}
				};
			});
		}

		public CastingMoldRecipe[] GetRecipes()
		{
			return Recipes;
		}

		public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
		{
			if(blockSel == null) return;

			BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite));

			IPlayer byPlayer = null;
			if(byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

			if(byPlayer != null && be is BlockEntityGlassCastingMold)
			{
				BlockEntityGlassCastingMold beim = (BlockEntityGlassCastingMold)be;
				if(beim.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition))
				{
					handHandling = EnumHandHandling.PreventDefault;
				}
			}
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			if(blockSel == null) return false;

			BlockEntityGlassCastingMold be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassCastingMold;

			if(be != null) be.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);

			return true;
		}

		public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
		{
			if(!byPlayer.Entity.Controls.Sneak)
			{
				failureCode = "onlywhensneaking";
				return false;
			}

			if(!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				return false;
			}

			Block belowBlock = world.BlockAccessor.GetBlock(blockSel.Position.DownCopy());

			if(belowBlock.CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP))
			{
				DoPlaceBlock(world, byPlayer, blockSel, itemstack);
				return true;
			}

			failureCode = "requiresolidground";

			return false;
		}

		public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
		{
			return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
		}

		public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			List<ItemStack> stacks = new List<ItemStack>();

			stacks.Add(new ItemStack(this));

			BlockEntityGlassCastingMold be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityGlassCastingMold;

			if(be != null)
			{
				ItemStack[] outstack = be.GetReadyMoldedStacks();
				if(outstack != null)
				{
					stacks.AddRange(outstack);
				}
			}

			return stacks.ToArray();
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
	}
}