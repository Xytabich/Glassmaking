using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking
{
	public interface IItemCrafter
	{
		bool PreventRecipeAssignment(IClientPlayer player, ItemStack item);

		bool TryGetRecipeOutputs(IClientPlayer player, ItemStack item, [NotNullWhen(true)] out KeyValuePair<IAttribute, ItemStack>[]? recipeOutputs);
	}
}