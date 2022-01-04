using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
    public interface IRecipeSourceItem
    {
        bool TryGetDialogParameters(out string DialogTitle, out KeyValuePair<IAttribute, ItemStack>[] recipeOutputs);
    }
}