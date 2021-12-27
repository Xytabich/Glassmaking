using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
    [JsonObject]
    public class GlassMoldRecipe
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public JsonItemStack output;
        [JsonProperty(Required = Required.Always)]
        public GlassBlend[] recipe;
    }
}