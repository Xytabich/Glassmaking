using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class LiquidUseBehavior : WorkbenchToolBehavior
	{
		public override string toolCode => "liquid";

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[toolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _, out _);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(!TryGetIngredient(world, recipe.Steps[step].Tools[toolCode], recipe.Code, out var item))
			{
				return false;
			}

			return TryGetItemSlot(byPlayer, item, out _, out _);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client) return;

			if(!TryGetIngredient(world, recipe.Steps[step].Tools[toolCode], recipe.Code, out var ingredient))
			{
				return;
			}

			if(TryGetItemSlot(byPlayer, ingredient, out var slot, out var source))
			{
				int quantity = (int)(source.GetContentProps(slot.Itemstack).ItemsPerLitre * ingredient.requiresLitres);
				source.TryTakeContent(slot.Itemstack, quantity);
				slot.MarkDirty();
			}
		}

		private bool TryGetIngredient(IWorldAccessor world, JsonObject json, AssetLocation recipeCode, out RequiredLiquid ingredient)
		{
			ingredient = json?.AsObject<RequiredLiquid>(null, recipeCode.Domain);
			if(ingredient == null)
			{
				world.Logger.Log(EnumLogType.Warning, "Unable to use liquid in workbench recipe '{0}' because json is malformed", recipeCode);
				return false;
			}
			return true;
		}

		private bool TryGetItemSlot(IPlayer byPlayer, RequiredLiquid required, out ItemSlot slot, out ILiquidSource source)
		{
			slot = byPlayer.InventoryManager?.ActiveHotbarSlot;
			var item = slot?.Itemstack;
			if(item != null && (source = item.Collectible as ILiquidSource) != null)
			{
				var content = source.GetContent(item);
				if(content != null && content.Collectible.Code.Equals(required.code) && content.Class == required.type)
				{
					return source.GetCurrentLitres(item) >= required.requiresLitres;
				}
			}

			slot = null;
			source = null;
			return false;
		}

		[JsonObject]
		private class RequiredLiquid
		{
			[JsonProperty(Required = Required.Always)]
			public AssetLocation code;
			public EnumItemClass type = EnumItemClass.Item;
			[JsonProperty(Required = Required.Always)]
			public float requiresLitres;
		}
	}
}