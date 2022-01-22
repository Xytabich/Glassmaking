using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace GlassMaking
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GlassMoldRecipe : IRecipeBase<GlassMoldRecipe>
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public CraftingRecipeIngredient output;
        [JsonProperty(Required = Required.Always)]
        public GlassAmount[] recipe;
        [JsonProperty]
        public float fillTime = 1f;

        [JsonProperty]
        public AssetLocation Name { get; set; }

        public bool Enabled { get; set; }
        public IRecipeIngredient[] Ingredients => new IRecipeIngredient[0];
        public IRecipeOutput Output => output.ReturnedStack;

        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();

            for(int i = 0; i < recipe.Length; i++)
            {
                if(!string.IsNullOrEmpty(recipe[i].name))
                {
                    var part = recipe[i];
                    int wildcardStartLen = part.code.Path.IndexOf("*");
                    if(wildcardStartLen >= 0)
                    {
                        List<string> codes = new List<string>();
                        int wildcardEndLen = part.code.Path.Length - wildcardStartLen - 1;
                        var mod = world.Api.ModLoader.GetModSystem<GlassMakingMod>();
                        foreach(var pair in mod.GetGlassTypes())
                        {
                            if(WildcardUtil.Match(output.Code, pair.Key))
                            {
                                string code = pair.Key.Path.Substring(wildcardStartLen);
                                string codepart = code.Substring(0, code.Length - wildcardEndLen);
                                if(part.allowedVariants == null || part.allowedVariants.Contains(codepart))
                                {
                                    codes.Add(codepart);
                                }
                            }
                        }
                        mappings[part.name] = codes.ToArray();
                    }
                }
            }

            return mappings;
        }

        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            throw new System.NotImplementedException();
        }

        public GlassMoldRecipe Clone()
        {
            return new GlassMoldRecipe() {
                output = output.Clone(),
                recipe = Array.ConvertAll(recipe, r => r.Clone()),
                fillTime = fillTime,
                Name = Name?.Clone()
            };
        }

        [JsonObject]
        public class GlassAmount
        {
            [JsonProperty]
            public string name;
            [JsonProperty]
            public string[] allowedVariants;
            [JsonProperty(Required = Required.DisallowNull)]
            public AssetLocation code;
            [JsonProperty(Required = Required.Always)]
            public int amount;
            [JsonProperty]
            public int var = -1;

            public bool IsSuitable(int amount)
            {
                if(amount < this.amount) return false;
                if(var > 0) return (amount - this.amount) <= var;
                return true;
            }

            public GlassAmount Clone()
            {
                return new GlassAmount() {
                    code = code.Clone(),
                    amount = amount,
                    var = var,
                    name = name
                };
            }
        }
    }
}