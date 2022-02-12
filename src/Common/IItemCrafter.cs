using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
	public interface IItemCrafter
	{
		bool PreventRecipeAssignment(IClientPlayer player, ItemStack item);

		bool TryGetRecipeOutputs(IClientPlayer player, ItemStack item, out KeyValuePair<IAttribute, ItemStack>[] recipeOutputs);
	}
}