using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class ItemUseBehavior : WorkbenchToolBehavior
	{
		public const string OTHER_CODE = "otherHandItem";
		public const string CODE = "handItem";

		public override string ToolCode { get; }

		private bool isOther;

		public ItemUseBehavior(bool isOther)
		{
			this.isOther = isOther;
			this.ToolCode = (isOther ? OTHER_CODE : CODE).ToLowerInvariant();
		}

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			return TryGetItemSlot(byPlayer, recipe.Steps[step].Tools[ToolCode]!, out _);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			return TryGetItemSlot(byPlayer, recipe.Steps[step].Tools[ToolCode]!, out _);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client) return;

			var ingredient = recipe.Steps[step].Tools[ToolCode]!;

			if(TryGetItemSlot(byPlayer, ingredient, out var slot))
			{
				if(ingredient.IsTool)
				{
					slot.Itemstack!.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, slot, ingredient.ToolDurabilityCost);
					return;
				}

				int quantity = ingredient.StackSize;
				slot.Itemstack!.StackSize -= quantity;
				if(slot.Itemstack.StackSize <= 0)
				{
					slot.Itemstack = null;
				}
				slot.MarkDirty();

				if(ingredient.ReturnedStack != null)
				{
					ItemStack item = ingredient.ReturnedStack.ResolvedItemStack!.Clone();
					if(!byPlayer.InventoryManager.TryGiveItemstack(item, true))
					{
						world.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ);
					}
				}
			}
		}

		public override WorldInteraction[]? GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, WorkbenchRecipe? recipe, int step)
		{
			if(recipe != null && recipe.Steps[step].Tools.TryGetValue(ToolCode, out var ingredient))
			{
				return [ new WorldInteraction() {
					Itemstacks = [ingredient!.ResolvedItemStack],
					MouseButton = EnumMouseButton.Right,
					ActionLangCode = "glassmaking:workbench-tool-item-use"
				} ];
			}
			return base.GetBlockInteractionHelp(world, selection, forPlayer, recipe, step);
		}

		private bool TryGetItemSlot(IPlayer byPlayer, CraftingRecipeIngredient required, [NotNullWhen(true)] out ItemSlot? slot)
		{
			ItemStack? item;
#pragma warning disable CS8762
			if(isOther)
			{
				slot = byPlayer.Entity?.RightHandItemSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;

				slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;
			}
			else
			{
				slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;

				slot = byPlayer.Entity?.RightHandItemSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;
			}
#pragma warning restore CS8762

			slot = null;
			return false;
		}
	}
}