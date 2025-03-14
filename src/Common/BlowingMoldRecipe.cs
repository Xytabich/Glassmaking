﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace GlassMaking
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class BlowingMoldRecipe : IRecipeBase<BlowingMoldRecipe>
	{
		[JsonProperty(Required = Required.DisallowNull)]
		public CraftingRecipeIngredient Output = default!;
		[JsonProperty(Required = Required.Always)]
		public GlassAmount[] Recipe = default!;
		[JsonProperty]
		public float FillTime = 3f;

		[JsonProperty]
		public AssetLocation? Name { get; set; }

		public bool Enabled { get; set; } = true;
		public IRecipeIngredient[] Ingredients => Recipe;
		IRecipeOutput IRecipeBase<BlowingMoldRecipe>.Output => Output.ReturnedStack;

		public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
		{
			Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

			var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
			for(int i = 0; i < Recipe.Length; i++)
			{
				if(!string.IsNullOrEmpty(Recipe[i].Name))
				{
					var part = Recipe[i];
					int wildcardStartLen = part.Code.Path.IndexOf("*");
					if(wildcardStartLen >= 0)
					{
						List<string> codes = new List<string>();
						int wildcardEndLen = part.Code.Path.Length - wildcardStartLen - 1;
						foreach(var pair in mod.GetGlassTypes())
						{
							if(WildcardUtil.Match(part.Code, pair.Key))
							{
								string code = pair.Key.Path.Substring(wildcardStartLen);
								string codepart = code.Substring(0, code.Length - wildcardEndLen);
								if(part.AllowedVariants == null || part.AllowedVariants.Contains(codepart))
								{
									codes.Add(codepart);
								}
							}
						}
						mappings[part.Name] = codes.ToArray();
					}
				}
			}

			return mappings;
		}

		public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
		{
			return Output.Resolve(world, sourceForErrorLogging);
		}

		public BlowingMoldRecipe Clone()
		{
			return new BlowingMoldRecipe() {
				Output = Output.Clone(),
				Recipe = Array.ConvertAll(Recipe, r => r.Clone()),
				FillTime = FillTime,
				Name = Name?.Clone()
			};
		}

		[JsonObject]
		public class GlassAmount : IRecipeIngredient
		{
			[JsonProperty]
			public string Name { get; set; } = default!;

			[JsonProperty(Required = Required.DisallowNull)]
			public AssetLocation Code { get; set; } = default!;

			[JsonProperty]
			public string[]? AllowedVariants;

			[JsonProperty(Required = Required.Always)]
			public int Amount;

			[JsonProperty]
			public int Var = -1;

			public bool IsSuitable(int amount)
			{
				if(amount < Amount) return false;
				if(Var > 0) return (amount - Amount) <= Var;
				return true;
			}

			public GlassAmount Clone()
			{
				return new GlassAmount() {
					Code = Code.Clone(),
					Amount = Amount,
					Var = Var,
					Name = Name,
					AllowedVariants = (string[]?)(AllowedVariants?.Clone())
				};
			}
		}
	}
}