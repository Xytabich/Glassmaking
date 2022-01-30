﻿using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
    public class BlockGlassBlowingMold : Block, IGlassBlowingMold
    {
        public GlassMoldRecipe[] recipes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            var recipe = Attributes?["glassmold"].AsObject<GlassMoldRecipe>(null, Code.Domain);
            if(recipe != null)
            {
                var world = api.World;
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
                    recipes = ArrayUtil.CreateFilled(qCombs, _ => recipe.Clone());
                    if(qCombs > 0)
                    {
                        foreach(var pair in nameToCodeMapping)
                        {
                            string variantCode = pair.Key;
                            string[] variants = pair.Value;

                            for(int i = 0; i < qCombs; i++)
                            {
                                var rec = recipes[i];

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
                    }
                    else
                    {
                        api.World.Logger.Warning("{0} mold make uses of wildcards, but no blocks or item matching those wildcards were found.", Code);
                    }
                }
                else
                {
                    recipes = new GlassMoldRecipe[] { recipe };
                }

                string source = Code.ToString();
                for(int i = 0; i < recipes.Length; i++)
                {
                    recipes[i].Resolve(world, source);
                }
            }
            else
            {
                recipes = new GlassMoldRecipe[0];
            }
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
            if(be != null) items = items.Append(be.GetDropItems() ?? new ItemStack[0]);
            return items;
        }

        public GlassMoldRecipe[] GetRecipes(IWorldAccessor world, ItemStack stack)
        {
            return recipes;
        }
    }
}