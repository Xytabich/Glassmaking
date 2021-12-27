using GlassMaking.Common;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public class BlockEntityGlassBlowingMold : BlockEntity, IGlassBlowingMold
    {
        private GlassMoldRecipe recipe = null;

        public bool CanReceiveGlass(int count, AssetLocation code)
        {
            var recipe = GetRecipe().recipe[0];
            return count >= recipe.amount && recipe.code.Equals(code);
        }

        public int GetRequiredAmount()
        {
            return GetRecipe().recipe[0].amount;
        }

        public ItemStack GetOutputItem()
        {
            var jstack = GetRecipe().output;
            if(jstack.ResolvedItemstack == null)
            {
                jstack.Resolve(Api.World, "glass mold output for " + Block.Code);
            }
            return jstack.ResolvedItemstack;
        }

        private GlassMoldRecipe GetRecipe()
        {
            if(recipe == null) recipe = Block.Attributes?["glassmold"]?.AsObject<GlassMoldRecipe>();
            return recipe;
        }
    }
}