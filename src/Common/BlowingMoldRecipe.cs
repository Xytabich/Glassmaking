using GlassMaking.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace GlassMaking
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class BlowingMoldRecipe : RecipeBase
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public CraftingRecipeIngredient Output = default!;
		[JsonProperty(Required = Required.Always)]
		public GlassIngredient[] Recipe = default!;
		[JsonProperty]
		public float FillTime = 3f;

		public override IEnumerable<IRecipeIngredient> RecipeIngredients => Recipe;
		public override IRecipeOutput RecipeOutput => Output;

		public override RecipeBase Clone()
		{
			var recipe = new BlowingMoldRecipe();
			CloneTo(recipe);
			return recipe;
		}

		public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			var types = world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes();
			foreach(var ingredient in Recipe)
			{
				var code = ingredient.Code;
				if(code == null || !types.ContainsKey(code))
				{
					world.Logger.Warning("Failed resolving a glass type with code '{0}' in {1}", code, sourceForErrorLogging);
					return false;
				}
			}
			return Output.Resolve(world, sourceForErrorLogging);
		}

		protected override Dictionary<string, HashSet<string>> GetNameToCodeMapping(IWorldAccessor world)
		{
			IReadOnlyDictionary<AssetLocation, GlassTypeVariant>? types = null;
			var mapping = new Dictionary<string, HashSet<string>>();
			foreach(var ingredient in Recipe)
			{
				ingredient.MatchingType = IRecipeIngredient.GetMatchType(ingredient.Code?.ToString(), ingredient.Name != null);
				switch(ingredient.MatchingType)
				{
					case EnumRecipeMatchType.NamedWildcard:
						types ??= world.Api.ModLoader.GetModSystem<GlassMakingMod>().GetGlassTypes();
						var list = Utils.WildcardMatches(ingredient.Code!, types.Keys, ingredient.AllowedVariants);
						if(list.Count != 0)
						{
							mapping[ingredient.Name!] = list;
						}
						break;
				}
			}

			return mapping;
		}

		protected override void CloneTo(object cloneTo)
		{
			base.CloneTo(cloneTo);
			if(cloneTo is BlowingMoldRecipe recipe)
			{
				recipe.Output = Output.Clone();
				recipe.Recipe = Array.ConvertAll(Recipe, r => r.Clone());
				recipe.FillTime = FillTime;
			}
		}

		[JsonObject]
		public class GlassIngredient : CraftingRecipeIngredient, IConcreteCloneable<GlassIngredient>
		{
			[JsonProperty]
			public int Amount { get => Quantity; set => Quantity = value; }

			[JsonProperty]
			public int Var = -1;

			public bool IsSuitable(int amount)
			{
				if(amount < Amount) return false;
				if(Var > 0) return (amount - Amount) <= Var;
				return true;
			}

			public new GlassIngredient Clone()
			{
				var ingredient = new GlassIngredient();
				CloneTo(ingredient);
				return ingredient;
			}

			protected override void CloneTo(object cloneTo)
			{
				base.CloneTo(cloneTo);
				if(cloneTo is GlassIngredient ingredient)
				{
					ingredient.Var = Var;
				}
			}
		}
	}
}