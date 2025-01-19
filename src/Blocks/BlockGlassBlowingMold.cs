using GlassMaking.Items;
using System;
using System.Collections.Generic;
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

				var tmpList = new List<BlowingMoldRecipe>();
				var attrib = Attributes["glassmaking:glassmold"];
				foreach(var recipe in (attrib.IsArray() ? attrib.AsObject<BlowingMoldRecipe[]>(null!, Code.Domain)
					: new BlowingMoldRecipe[] { attrib.AsObject<BlowingMoldRecipe>(null!, Code.Domain) }))
				{
					if(recipe != null && recipe.Enabled)
					{
						var nameToCodeMapping = recipe.GetNameToCodeMapping(world);
						if(nameToCodeMapping.Count > 0)
						{
							int qCombs = 0;
							bool first = true;
							foreach(var pair in nameToCodeMapping)
							{
								if(first) qCombs = pair.Value.Length;
								else qCombs *= pair.Value.Length;
								first = false;
							}
							if(qCombs > 0)
							{
								tmpList.Clear();
								for(int i = 0; i < qCombs; i++)
								{
									tmpList.Add(recipe.Clone());
								}
								foreach(var pair in nameToCodeMapping)
								{
									string variantCode = pair.Key;
									string[] variants = pair.Value;

									for(int i = 0; i < qCombs; i++)
									{
										var rec = tmpList[i];

										if(rec.Ingredients != null)
										{
											foreach(var ingred in rec.Ingredients)
											{
												if(ingred.Name == variantCode)
												{
													ingred.Code = ingred.Code.CopyWithPath(ingred.Code.Path.Replace("*", variants[i % variants.Length]));
												}
											}
										}

										rec.Output.FillPlaceHolder(variantCode, variants[i % variants.Length]);
									}
								}
								recipes.AddRange(tmpList);
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