﻿using Cairo;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
    public class BlowingMoldOutputInfo : IDisposable
    {
        private GlassMakingMod mod;

        public BlowingMoldOutputInfo(GlassMakingMod mod)
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
            if(mod.TryGetMoldsForItem(inSlot.Itemstack.Collectible, out var blocks))
            {
                outComponents.Add(new ClearFloatTextComponent(capi, 7f));
                outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Blown in a glass mold") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                foreach(var mold in blocks)
                {
                    var element = new ItemstackTextComponent(capi, new ItemStack(mold), 40.0, 10.0,
                        EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                    element.offY = GuiElement.scaled(7.0);
                    outComponents.Add(element);
                }
                outComponents.Add(new ClearFloatTextComponent(capi, 7f));
            }
        }
    }
}