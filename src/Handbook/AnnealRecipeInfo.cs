﻿using GlassMaking.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
	public class AnnealRecipeInfo : IDisposable
	{
		public AnnealRecipeInfo()
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
			var collectible = inSlot.Itemstack.Collectible;
			if(collectible.Attributes != null && collectible.Attributes.KeyExists("glassmaking:anneal"))
			{
				var properties = collectible.Attributes["glassmaking:anneal"];
				var output = properties["output"].AsObject<JsonItemStack>(null!, collectible.Code.Domain);
				if(output.Resolve(capi.World, "handbook recipe"))
				{
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
					outComponents.AddHandbookBoldRichText(capi, Lang.Get("glassmaking:Turns out when annealing") + "\n", openDetailPageFor);
					var element = new ItemstackTextComponent(capi, output.ResolvedItemstack, 40.0, 10.0,
						EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
					outComponents.Add(element);

					outComponents.Add(new ClearFloatTextComponent(capi));
					var annealTemperature = properties["temperature"].AsObject<MinMaxFloat>();
					var annealTime = properties["time"].AsInt() / 3600.0;
					outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Heat up to {0}, then keep the temperature above {1} for {2} hours",
						annealTemperature.Max.ToString("0"), annealTemperature.Min.ToString("0"), annealTime.ToString("G", CultureInfo.InvariantCulture)) + "\n", CairoFont.WhiteSmallText()));

					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				}
			}
		}
	}
}