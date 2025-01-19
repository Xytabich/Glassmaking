using GlassMaking.Common;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.Items.Behavior
{
	public class ItemBehaviorGlassBlend : CollectibleBehavior
	{
		public ItemBehaviorGlassBlend(CollectibleObject collObj) : base(collObj)
		{
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			GlassBlend? blend = GlassBlend.FromJson(inSlot.Itemstack);
			if(blend == null) blend = GlassBlend.FromTreeAttributes(inSlot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null && blend.Amount > 0)
			{
				dsc.AppendLine(Lang.Get("glassmaking:Melts into {0} units of {1} glass", blend.Amount, Lang.Get(GlassBlend.GetBlendNameCode(blend.Code))));
			}
		}
	}
}