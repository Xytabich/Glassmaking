using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
    public interface IGlassBlowingMold
    {
        GlassMoldRecipe[] GetRecipes(IWorldAccessor world, ItemStack stack);
    }
}