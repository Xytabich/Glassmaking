using Vintagestory.API.Common;

namespace GlassMaking.Items.Behavior
{
	public interface ICraftingGridBehavior
	{
		bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient, ref EnumHandling handling);

		void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity, ref EnumHandling handling);
	}
}