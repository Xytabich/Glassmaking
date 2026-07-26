using GlassMaking.Blocks;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
	public class CastingMoldRecipeInfo : IDisposable
	{
		public CastingMoldRecipeInfo()
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
			if(inSlot.Itemstack!.Collectible is IGlassCastingMold mold)
			{
				var recipes = mold.GetRecipes();
				if(recipes != null && recipes.Length > 0)
				{
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
					outComponents.AddHandbookBoldRichText(capi, Lang.Get("glassmaking:Mold for") + "\n", openDetailPageFor);
					foreach(var recipe in recipes)
					{
						if(recipe.Output.ResolvedItemStack != null)
						{
							var element = new ItemstackTextComponent(capi, recipe.Output.ResolvedItemStack, 40.0, 10.0,
								EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
							element.ShowStacksize = recipe.Output.ResolvedItemStack.StackSize > 1;
							outComponents.Add(element);

							outComponents.Add(new ClearFloatTextComponent(capi, 3f));
							outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Cast from {1} units of {0} glass",
								Lang.Get(GlassBlend.GetBlendNameCode(recipe.Recipe.Code!)), recipe.Recipe.Quantity) + "\n", CairoFont.WhiteSmallText()));
						}
					}
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				}
			}
		}
	}
}