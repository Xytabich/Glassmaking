using Cairo;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.Handbook
{
	internal class ItemMeltableInfo : IDisposable
	{
		private GlassMakingMod mod;

		public ItemMeltableInfo(GlassMakingMod mod)
		{
			this.mod = mod;
			HandbookItemInfoEvent.onGetHandbookInfo += GetHandbookInfo;
		}

		public void Dispose()
		{
			HandbookItemInfoEvent.onGetHandbookInfo -= GetHandbookInfo;
		}

		private void GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, HandbookItemInfoSection section, List<RichTextComponentBase> outComponents)
		{
			if(section != HandbookItemInfoSection.AfterItemHeader) return;

			GlassBlend blend = GlassBlend.FromJson(inSlot.Itemstack);
			if(blend == null) blend = GlassBlend.FromTreeAttributes(inSlot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null && blend.amount > 0)
			{
				outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Melting in the glass smeltery") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
				outComponents.AddRange(VtmlUtil.Richtextify(capi, Lang.Get("{0} units of {1} glass at {2}°C", blend.amount, Lang.Get(GlassBlend.GetBlendNameCode(blend.code)), mod.GetGlassTypeInfo(blend.code)?.meltingPoint.ToString("0")) + "\n", CairoFont.WhiteSmallText()));
			}
		}
	}
}