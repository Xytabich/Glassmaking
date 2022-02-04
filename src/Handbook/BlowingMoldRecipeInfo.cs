using Cairo;
using GlassMaking.Blocks;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
    public class BlowingMoldRecipeInfo : IDisposable
    {
        public BlowingMoldRecipeInfo()
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
            if(inSlot.Itemstack.Collectible is IGlassBlowingMold mold)
            {
                var recipes = mold.GetRecipes();
                if(recipes != null && recipes.Length > 0)
                {
                    outComponents.Add(new ClearFloatTextComponent(capi, 7f));
                    outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Mold for") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
                    foreach(var recipe in recipes)
                    {
                        if(recipe.output.ResolvedItemstack != null)
                        {
                            var element = new ItemstackTextComponent(capi, recipe.output.ResolvedItemstack, 40.0, 10.0,
                                EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
                            element.offY = GuiElement.scaled(7.0);
                            outComponents.Add(element);
                        }
                    }
                    outComponents.Add(new ClearFloatTextComponent(capi, 7f));
                }
            }
        }
    }
}