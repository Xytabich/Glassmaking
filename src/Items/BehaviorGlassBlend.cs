using GlassMaking.Common;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.Items
{
	public class BehaviorGlassBlend : CollectibleBehavior
	{
		public BehaviorGlassBlend(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			GlassBlend blend = GlassBlend.FromJson(inSlot.Itemstack);
			if(blend == null) blend = GlassBlend.FromTreeAttributes(inSlot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null && blend.amount > 0)
			{
				dsc.AppendLine(Lang.Get("glassmaking:Melts into {0} units of {1} glass", blend.amount, Lang.Get(GlassBlend.GetBlendNameCode(blend.code))));
			}
		}
	}
}