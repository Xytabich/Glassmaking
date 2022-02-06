using Cairo;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
    public class AnnealOutputInfo : IDisposable
    {
        private GlassMakingMod mod;

        public AnnealOutputInfo(GlassMakingMod mod)
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
            if(section != HandbookItemInfoSection.BeforeExtraSections) return;
            if(mod.TryGetMaterialsForAnneal(inSlot.Itemstack, out var materials))
            {
                outComponents.Add(new ClearFloatTextComponent(capi, 7f));
                outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Obtained by annealing") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                foreach(var material in materials)
                {
                    var element = new ItemstackTextComponent(capi, new ItemStack(material), 40.0, 10.0,
                        EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                    element.offY = GuiElement.scaled(7.0);
                    outComponents.Add(element);
                }
                outComponents.Add(new ClearFloatTextComponent(capi, 7f));
            }
        }
    }
}