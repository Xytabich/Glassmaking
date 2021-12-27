using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Common
{
    [JsonObject]
    public class GlassBlend
    {
        public const string PROPERTY_NAME = "glassblend";

        [JsonProperty(Required = Required.DisallowNull)]
        public AssetLocation code;
        [JsonProperty(Required = Required.Always)]
        public int amount;

        public GlassBlend()
        {
        }

        public GlassBlend(AssetLocation code, int amount)
        {
            this.code = code;
            this.amount = amount;
        }

        public void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("code", code.ToShortString());
            tree.SetInt("amount", amount);
        }

        public static string GetBlendNameCode(AssetLocation code)
        {
            return code.Clone().WithPathPrefixOnce("glassblend-").ToShortString();
        }

        public static GlassBlend FromTreeAttributes(ITreeAttribute tree)
        {
            if(tree == null) return null;
            return new GlassBlend(new AssetLocation(tree.GetString("code")), tree.GetInt("amount"));
        }
    }
}