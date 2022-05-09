using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace GlassMaking
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class CastingMoldRecipe : IRecipeBase<CastingMoldRecipe>
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public CraftingRecipeIngredient Output;
		[JsonProperty(Required = Required.Always)]
		public GlassAmount Recipe;

		[JsonProperty]
		public AssetLocation Name { get; set; }

		public bool Enabled { get; set; } = true;
		public IRecipeIngredient[] Ingredients => new IRecipeIngredient[] { Recipe };
		IRecipeOutput IRecipeBase<CastingMoldRecipe>.Output => Output.ReturnedStack;

		public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
		{
			Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

			if(!string.IsNullOrEmpty(Recipe.Name))
			{
				int wildcardStartLen = Recipe.Code.Path.IndexOf("*");
				if(wildcardStartLen >= 0)
				{
					List<string> codes = new List<string>();
					int wildcardEndLen = Recipe.Code.Path.Length - wildcardStartLen - 1;
					var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
					foreach(var pair in mod.GetGlassTypes())
					{
						if(WildcardUtil.Match(Recipe.Code, pair.Key))
						{
							string code = pair.Key.Path.Substring(wildcardStartLen);
							string codepart = code.Substring(0, code.Length - wildcardEndLen);
							if(Recipe.AllowedVariants == null || Recipe.AllowedVariants.Contains(codepart))
							{
								codes.Add(codepart);
							}
						}
					}
					mappings[Recipe.Name] = codes.ToArray();
				}
			}

			return mappings;
		}

		public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			return Output.Resolve(world, sourceForErrorLogging);
		}

		public CastingMoldRecipe Clone()
		{
			return new CastingMoldRecipe()
			{
				Output = Output.Clone(),
				Recipe = Recipe.Clone(),
				Name = Name?.Clone()
			};
		}

		[JsonObject]
		public class GlassAmount : IRecipeIngredient
		{
			public string Name => "type";
			[JsonProperty]
			public string[] AllowedVariants;
			[JsonProperty(Required = Required.DisallowNull)]
			public AssetLocation Code { get; set; }
			[JsonProperty(Required = Required.Always)]
			public int Amount;

			public GlassAmount Clone()
			{
				return new GlassAmount()
				{
					Code = Code.Clone(),
					Amount = Amount,
					AllowedVariants = (string[])(AllowedVariants?.Clone())
				};
			}
		}
	}
}