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
        public GlassAmount[] recipe;
        [JsonProperty]
        public float fillTime = 1f;

        [JsonObject]
        public class GlassAmount
        {
            [JsonProperty(Required = Required.DisallowNull)]
            public AssetLocation code;
            [JsonProperty(Required = Required.Always)]
            public int amount;
            [JsonProperty]
            public int var = -1;

            public bool IsSuitable(int amount)
            {
                if(amount < this.amount) return false;
                if(var > 0) return (this.amount + var) <= amount;
                return true;
            }
        }
    }
}