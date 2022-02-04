using Cairo;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
    public class TemperingRecipeInfo : IDisposable
    {
        public TemperingRecipeInfo()
        {
            HandbookItemInfoEvent.onGetHandbookInfo += GetHandbookInfo;
        }

        public void Dispose()
        {
            HandbookItemInfoEvent.onGetHandbookInfo -= GetHandbookInfo;
        }

        private void GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, HandbookItemInfoSection section, List<RichTextComponentBase> outComponents)
        {
            if(section != HandbookItemInfoSection.BeforeExtraSections) return;
            var collectible = inSlot.Itemstack.Collectible;
            if(collectible.Attributes != null && collectible.Attributes.KeyExists("glassmaking:tempering"))
            {
                var properties = collectible.Attributes["glassmaking:tempering"];
                var output = properties["output"].AsObject<JsonItemStack>(null, collectible.Code.Domain);
                if(output.Resolve(capi.World, "handbook recipe"))
                {
                    outComponents.Add(new ClearFloatTextComponent(capi, 7f));
                    outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Turns out when tempering") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                    var element = new ItemstackTextComponent(capi, output.ResolvedItemstack, 40.0, 10.0,
                        EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                    element.offY = GuiElement.scaled(7.0);
                    outComponents.Add(element);

                    outComponents.Add(new ClearFloatTextComponent(capi));
                    var temperingTemperature = properties["temperature"].AsObject<MinMaxFloat>();
                    var temperingTime = properties["time"].AsInt() / 3600.0;
                    outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Heat up to {0}, then keep the temperature above {1} for {2} hours",
                        temperingTemperature.max.ToString("0"), temperingTemperature.min.ToString("0"), temperingTime.ToString("0.0")) + "\n", CairoFont.WhiteSmallText()));

                    outComponents.Add(new ClearFloatTextComponent(capi, 7f));
                }
            }
        }
    }
}