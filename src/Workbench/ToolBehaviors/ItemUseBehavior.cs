using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class ItemUseBehavior : WorkbenchToolBehavior
	{
		public const string OTHER_CODE = "otherHandItem";
		public const string CODE = "handItem";

		public override string toolCode { get; }

		public ItemUseBehavior(bool isOther)
		{
			this.toolCode = (isOther ? OTHER_CODE : CODE).ToLowerInvariant();
		}

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[toolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[toolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[toolCode], recipe.Code, out var ingredient))
			{
				return;
			}

			if(TryGetItemSlot(byPlayer, ingredient, out var slot))
			{
				if(ingredient.IsTool)
				{
					slot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, slot, ingredient.ToolDurabilityCost);
					return;
				}

				int quantity = (ingredient.IsWildCard ? ingredient.Quantity : ingredient.ResolvedItemstack.StackSize);
				slot.Itemstack.StackSize -= quantity;
				if(slot.Itemstack.StackSize <= 0)
				{
					slot.Itemstack = null;
					slot.MarkDirty();
				}

				if(ingredient.ReturnedStack != null)
				{
					ItemStack item = ingredient.ReturnedStack.ResolvedItemstack.Clone();
					if(!byPlayer.InventoryManager.TryGiveItemstack(item, true))
					{
						world.SpawnItemEntity(item, byPlayer.Entity.Pos.XYZ);
					}
				}
			}
		}

		private bool TryGetIngredient(IWorldAccessor world, JsonObject json, AssetLocation recipeCode, out CraftingRecipeIngredient item)
		{
			item = json?.AsObject<CraftingRecipeIngredient>(null, recipeCode.Domain);
			if(item == null)
			{
				world.Logger.Log(EnumLogType.Warning, "Unable to use item in workbench recipe '{0}' because json is malformed", recipeCode);
				return false;
			}
			return item.Resolve(world, "workbench recipe item");
		}

		private bool TryGetItemSlot(IPlayer byPlayer, CraftingRecipeIngredient required, out ItemSlot slot)
		{
			ItemStack item;
			if(toolCode == CODE)
			{
				slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;

				slot = byPlayer.Entity?.RightHandItemSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;
			}
			else
			{
				slot = byPlayer.Entity?.RightHandItemSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;

				slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
				item = slot?.Itemstack;
				if(item != null && required.SatisfiesAsIngredient(item)) return true;
			}

			slot = null;
			return false;
		}
	}
}