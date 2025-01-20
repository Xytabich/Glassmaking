using Cairo;
using GlassMaking.Items;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
	public class AllowedLiquidsList : IDisposable
	{
		public AllowedLiquidsList()
		{
			HandbookItemInfoEvent.OnGetHandbookInfo += GetHandbookInfo;
		}

		public void Dispose()
		{
			HandbookItemInfoEvent.OnGetHandbookInfo -= GetHandbookInfo;
		}

		private void GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, HandbookItemInfoSection section, List<RichTextComponentBase> outComponents)
		{
			if(section != HandbookItemInfoSection.BeforeExtraSections) return;
			if(inSlot.Itemstack.Collectible is StrictLiquidContainer container)
			{
				outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Allowed liquids") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
				foreach(var liquid in container.AllowedLiquids)
				{
					var element = new ItemstackTextComponent(capi, liquid, 40.0, 8, EnumFloat.Inline,
						cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
					outComponents.Add(element);
				}
				outComponents.Add(new ClearFloatTextComponent(capi, 7f));
			}
		}
	}
}