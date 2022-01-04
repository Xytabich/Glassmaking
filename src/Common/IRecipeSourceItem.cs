using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
    public interface IRecipeSourceItem
    {
        bool TryGetRecipeOutputs(out KeyValuePair<IAttribute, ItemStack>[] recipeOutputs);
    }
}