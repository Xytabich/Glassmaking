using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[ToolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[ToolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client) return;

			if(!TryGetIngredient(world, recipe.Steps[step].Tools[ToolCode], recipe.Code, out var ingredient))
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
				}
				slot.MarkDirty();

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

		public override WorldInteraction[]? GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, WorkbenchRecipe? recipe, int step)
		{
			if(recipe != null && recipe.Steps[step].Tools.TryGetValue(ToolCode, out var json))
			{
				if(TryGetIngredient(world, json, recipe.Code, out var ingredient))
				{
					return new WorldInteraction[] { new WorldInteraction() {
						Itemstacks = new ItemStack[] { ingredient.ResolvedItemstack },
						MouseButton = EnumMouseButton.Right,
						ActionLangCode = "glassmaking:workbench-tool-item-use"
					} };
				}
			}
			return base.GetBlockInteractionHelp(world, selection, forPlayer, recipe, step);
		}

		private bool TryGetIngredient(IWorldAccessor world, JsonObject? json, AssetLocation recipeCode, [NotNullWhen(true)] out CraftingRecipeIngredient? ingredient)
		{
			ingredient = json?.AsObject<CraftingRecipeIngredient?>(null, recipeCode.Domain);
			if(ingredient == null)
			{
				world.Logger.Log(EnumLogType.Warning, "Unable to use item in workbench recipe '{0}' because json is malformed", recipeCode);
				return false;
			}
			return ingredient.Resolve(world, "workbench recipe item");
		}

		private bool TryGetItemSlot(IPlayer byPlayer, CraftingRecipeIngredient required, [NotNullWhen(true)] out ItemSlot? slot)
		{
			ItemStack? item;
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

			slot = null;
			return false;
		}
	}
}