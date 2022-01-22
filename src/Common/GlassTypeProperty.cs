using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace GlassMaking.Common
{
    public class GlassTypeProperty : WorldProperty<GlassTypeVariant>
    {

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GlassTypeVariant : WorldPropertyVariant
    {

    }
}