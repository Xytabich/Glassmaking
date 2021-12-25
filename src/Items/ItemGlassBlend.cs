using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.Items
{
    public class ItemGlassBlend : Item
    {
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            if(Attributes["glassunits"] != null)
            {
                dsc.AppendLine(Lang.Get("glassmaking:melts into {0} units of {1}", Attributes["glassunits"].AsInt(), Lang.Get("Glass")));
            }
        }
    }
}
