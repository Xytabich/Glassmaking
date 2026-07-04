using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class LiquidUseBehavior : WorkbenchToolBehavior
	{
		public const string CODE = "liquid";

		public override string ToolCode => CODE;

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			return TryGetItemSlot(byPlayer, recipe.Steps[step].Tools[ToolCode]!, out _, out _);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			return TryGetItemSlot(byPlayer, recipe.Steps[step].Tools[ToolCode]!, out _, out _);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client) return;

			var ingredient = recipe.Steps[step].Tools[ToolCode]!;

			if(TryGetItemSlot(byPlayer, ingredient, out var slot, out var source))
			{
				int quantity = (int)(source.GetContentProps(slot.Itemstack!)!.ItemsPerLitre * RequiresLitres(ingredient));
				source.TryTakeContent(slot.Itemstack!, quantity);
				slot.MarkDirty();
			}
		}

		public override WorldInteraction[]? GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, WorkbenchRecipe? recipe, int step)
		{
			if(recipe != null && recipe.Steps[step].Tools.TryGetValue(ToolCode, out var ingredient))
			{
				var itemStack = ingredient!.ResolvedItemStack!.Clone();
				itemStack.StackSize = (int)(BlockLiquidContainerBase.GetContainableProps(itemStack)!.ItemsPerLitre * RequiresLitres(ingredient));
				return [ new WorldInteraction() {
					Itemstacks = [itemStack],
					MouseButton = EnumMouseButton.Right,
					ActionLangCode = "glassmaking:workbench-tool-liquid-use"
				} ];
			}
			return base.GetBlockInteractionHelp(world, selection, forPlayer, recipe, step);
		}

		private bool TryGetItemSlot(IPlayer byPlayer, CraftingRecipeIngredient ingredient, [NotNullWhen(true)] out ItemSlot? slot, [NotNullWhen(true)] out ILiquidSource? source)
		{
			slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
			var item = slot?.Itemstack;
			if(item != null && (source = item.Collectible as ILiquidSource) != null)
			{
				var content = source.GetContent(item);
				if(content != null && content.Collectible.Code.Equals(ingredient.Code) && content.Class == ingredient.Type)
				{
					return source.GetCurrentLitres(item) >= RequiresLitres(ingredient);
				}
			}

			slot = null;
			source = null;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float RequiresLitres(CraftingRecipeIngredient ingredient)
		{
			return ingredient.RecipeAttributes!["requiresLitres"].AsFloat();
		}
	}
}