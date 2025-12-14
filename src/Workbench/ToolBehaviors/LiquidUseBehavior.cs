using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class LiquidUseBehavior : WorkbenchToolBehavior
	{
		public const string CODE = "liquid";

		public override string ToolCode => CODE;

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[ToolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _, out _);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[ToolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _, out _);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client) return;

			if(!TryGetIngredient(world, recipe.Steps[step].Tools[ToolCode], recipe.Code, out var ingredient))
			{
				return;
			}

			if(TryGetItemSlot(byPlayer, ingredient, out var slot, out var source))
			{
				int quantity = (int)(source.GetContentProps(slot.Itemstack)!.ItemsPerLitre * ingredient.RequiresLitres);
				source.TryTakeContent(slot.Itemstack, quantity);
				slot.MarkDirty();
			}
		}

		public override WorldInteraction[]? GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, WorkbenchRecipe? recipe, int step)
		{
			if(recipe != null && recipe.Steps[step].Tools.TryGetValue(ToolCode, out var json))
			{
				if(TryGetIngredient(world, json, recipe.Code, out var ingredient))
				{
					var itemStack = ingredient.Type == EnumItemClass.Item ? new ItemStack(world.GetItem(ingredient.Code)) : new ItemStack(world.GetBlock(ingredient.Code));
					itemStack.StackSize = (int)(BlockLiquidContainerBase.GetContainableProps(itemStack)!.ItemsPerLitre * ingredient.RequiresLitres);
					return new WorldInteraction[] { new WorldInteraction() {
						Itemstacks = new ItemStack[] { itemStack },
						MouseButton = EnumMouseButton.Right,
						ActionLangCode = "glassmaking:workbench-tool-liquid-use"
					} };
				}
			}
			return base.GetBlockInteractionHelp(world, selection, forPlayer, recipe, step);
		}

		private bool TryGetIngredient(IWorldAccessor world, JsonObject? json, AssetLocation recipeCode, [NotNullWhen(true)] out RequiredLiquid? ingredient)
		{
			ingredient = json?.AsObject<RequiredLiquid?>(null, recipeCode.Domain);
			if(ingredient == null)
			{
				world.Logger.Log(EnumLogType.Warning, "Unable to use liquid in workbench recipe '{0}' because json is malformed", recipeCode);
				return false;
			}
			return true;
		}

		private bool TryGetItemSlot(IPlayer byPlayer, RequiredLiquid required, [NotNullWhen(true)] out ItemSlot? slot, [NotNullWhen(true)] out ILiquidSource? source)
		{
			slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
			var item = slot?.Itemstack;
			if(item != null && (source = item.Collectible as ILiquidSource) != null)
			{
				var content = source.GetContent(item);
				if(content != null && content.Collectible.Code.Equals(required.Code) && content.Class == required.Type)
				{
					return source.GetCurrentLitres(item) >= required.RequiresLitres;
				}
			}

			slot = null;
			source = null;
			return false;
		}

		[JsonObject]
		public sealed class RequiredLiquid
		{
			[JsonProperty(Required = Required.Always)]
			public AssetLocation Code = default!;

			public EnumItemClass Type = EnumItemClass.Item;

			[JsonProperty(Required = Required.Always)]
			public float RequiresLitres;
		}
	}
}