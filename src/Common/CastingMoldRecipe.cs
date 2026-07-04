using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace GlassMaking
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class CastingMoldRecipe : RecipeBase
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public JsonItemStack Output = default!;

		[JsonProperty(Required = Required.DisallowNull)]
		public GlassIngredient Recipe = default!;

		public override IEnumerable<IRecipeIngredient> RecipeIngredients => ingredients ??= [Recipe];
		public override IRecipeOutput RecipeOutput => Output;

		private IRecipeIngredient[]? ingredients = null;

		public override IEnumerable<IRecipeBase> GenerateRecipesForAllIngredientCombinations(IWorldAccessor world)
		{
			Recipe.MatchingType = IRecipeIngredient.GetMatchType(Recipe.Code?.ToString(), false);
			if(Recipe.MatchingType == EnumRecipeMatchType.Wildcard)
			{
				var types = world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes();
				return Utils.WildcardMatches(Recipe.Code!, types.Keys, Recipe.AllowedVariants).Select(str => {
					var recipe = new CastingMoldRecipe();
					CloneTo(recipe);
					recipe.Recipe.Code!.Path = recipe.Recipe.Code!.Path?.Replace("*", str);
					recipe.Output.Code?.Path = recipe.Output.Code?.Path?.Replace("*", str);
					return recipe;
				});
			}
			return [this];
		}

		public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			var code = Recipe.Code;
			if(code == null || !world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes().ContainsKey(code))
			{
				world.Logger.Warning("Failed resolving a glass type with code '{0}' in {1}", code, sourceForErrorLogging);
				return false;
			}
			return Output.Resolve(world, sourceForErrorLogging);
		}

		public override CastingMoldRecipe Clone()
		{
			var recipe = new CastingMoldRecipe();
			CloneTo(recipe);
			return recipe;
		}

		protected override void CloneTo(object cloneTo)
		{
			base.CloneTo(cloneTo);
			if(cloneTo is CastingMoldRecipe recipe)
			{
				recipe.Output = Output.Clone();
				recipe.Recipe = Recipe.Clone();
			}
		}

		[JsonObject]
		public class GlassIngredient : CraftingRecipeIngredient, IConcreteCloneable<GlassIngredient>
		{
			[JsonProperty]
			public int Amount { get => Quantity; set => Quantity = value; }

			public bool IsSuitable(int amount)
			{
				if(amount < Amount) return false;
				return true;
			}

			public new GlassIngredient Clone()
			{
				var ingredient = new GlassIngredient();
				CloneTo(ingredient);
				return ingredient;
			}
		}
	}
}