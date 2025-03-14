﻿using GlassMaking.Blocks;
using GlassMaking.Common;
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
			HandbookItemInfoEvent.OnGetHandbookInfo += GetHandbookInfo;
		}

		public void Dispose()
		{
			HandbookItemInfoEvent.OnGetHandbookInfo -= GetHandbookInfo;
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
					outComponents.AddHandbookBoldRichText(capi, Lang.Get("glassmaking:Mold for") + "\n", openDetailPageFor);
					foreach(var recipe in recipes)
					{
						if(recipe.Output.ResolvedItemstack != null)
						{
							var element = new SlideshowItemstackTextComponent(capi, new ItemStack[] { recipe.Output.ResolvedItemstack }, 40.0,
								EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
							element.ShowStackSize = recipe.Output.ResolvedItemstack.StackSize > 1;
							element.PaddingRight = GuiElement.scaled(10.0);
							outComponents.Add(element);

							outComponents.Add(new ClearFloatTextComponent(capi));
							outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Mold layers:") + "\n", CairoFont.WhiteSmallText()));
							foreach(var layer in recipe.Recipe)
							{
								if(layer.Var > 0)
								{
									outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:{0} glass {1}-{2} units",
										Lang.Get(GlassBlend.GetBlendNameCode(layer.Code)), layer.Amount, layer.Amount + layer.Var) + "\n", CairoFont.WhiteSmallText()));
								}
								else
								{
									outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:{0} glass {1} units",
										Lang.Get(GlassBlend.GetBlendNameCode(layer.Code)), layer.Amount) + "\n", CairoFont.WhiteSmallText()));
								}
							}
						}
					}
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				}
			}
		}
	}
}