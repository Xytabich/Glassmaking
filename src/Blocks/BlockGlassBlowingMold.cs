using GlassMaking.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockGlassBlowingMold : Block, IGlassBlowingMold
	{
		public BlowingMoldRecipe[] Recipes = default!;

		private WorldInteraction[] interactions = default!;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			if(Attributes.KeyExists("glassmaking:glassmold"))
			{
				var world = api.World;
				var recipes = new List<BlowingMoldRecipe>();

				var attrib = Attributes["glassmaking:glassmold"];
				foreach(var recipe in attrib.AsArrayOrSingle<BlowingMoldRecipe>([]))
				{
					if(recipe != null && recipe.Enabled)
					{
						recipes.AddRange(recipe.GenerateRecipesForAllIngredientCombinations(world).Select(r => (BlowingMoldRecipe)r));
					}
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
				Recipes = new BlowingMoldRecipe[0];
			}

			if(api.Side != EnumAppSide.Client) return;
			interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:blowingmoldinteractions", () => {
				List<ItemStack> smeltedContainerStacks = new List<ItemStack>();

				foreach(CollectibleObject obj in api.World.Items)
				{
					if(obj is ItemGlassworkPipe)
					{
						smeltedContainerStacks.Add(new ItemStack(obj));
					}
				}

				return new WorldInteraction[] {
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-blowingmold-fill",
						HotKeyCode = null,
						MouseButton = EnumMouseButton.Right,
						Itemstacks = smeltedContainerStacks.ToArray(),
						GetMatchingStacks = (wi, bs, es) =>
						{
							var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGlassBlowingMold;
							return (be != null && be.CanBeFilled) ? wi.Itemstacks : null;
						}
					},
					new WorldInteraction()
					{
						ActionLangCode = "glassmaking:blockhelp-blowingmold-takeitem",
						HotKeyCode = null,
						RequireFreeHand = true,
						MouseButton = EnumMouseButton.Right,
						ShouldApply = (wi, bs, es) =>
						{
							var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGlassBlowingMold;
							return be != null && be.CanTakeItem;
						}
					}
				};
			});
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			if(blockSel != null)
			{
				var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassBlowingMold;
				if(be != null)
				{
					if(be.OnInteract(world, byPlayer))
					{
						return true;
					}
				}
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}

		public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			var items = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if(items == null) items = new ItemStack[0];
			var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityGlassBlowingMold;
			if(be != null) items = items.Append(be.GetDropItems() ?? Array.Empty<ItemStack>());
			return items;
		}

		public BlowingMoldRecipe[] GetRecipes()
		{
			return Recipes;
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}
	}
}