using GlassMaking.Workbench;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
	public class WorkbenchRecipeInfo : IDisposable
	{
		private GlassMakingMod mod;

		public WorkbenchRecipeInfo(GlassMakingMod mod)
		{
			this.mod = mod;
			HandbookItemInfoEvent.OnGetHandbookInfo += GetHandbookInfo;
		}

		public void Dispose()
		{
			HandbookItemInfoEvent.OnGetHandbookInfo -= GetHandbookInfo;
		}

		private void GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, HandbookItemInfoSection section, List<RichTextComponentBase> outComponents)
		{
			if(section != HandbookItemInfoSection.BeforeExtraSections) return;
			var itemstack = inSlot.Itemstack!;
			List<WorkbenchRecipe>? recipes = null;
			foreach(var recipe in mod.GetWorkbenchRecipes())
			{
				if(recipe.Value.Output.ResolvedItemStack != null && recipe.Value.Output.ResolvedItemStack.Equals(capi.World, itemstack, GlobalConstants.IgnoredStackAttributes))
				{
					if(recipes == null) recipes = new List<WorkbenchRecipe>();
					recipes.Add(recipe.Value);
				}
			}
			if(recipes != null)
			{
				var toolItems = WorkbenchToolUtils.GetItemsByToolCode(capi);

				outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				outComponents.AddHandbookBoldRichText(capi, Lang.Get("glassmaking:Crafted at the glassmaker's workbench") + "\n", openDetailPageFor);
				for(int i = 0; i < recipes.Count; i++)
				{
					if(recipes.Count > 1)
					{
						outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Steps for recipe {0}", i + 1) + "\n", CairoFont.WhiteSmallText()));
					}
					var recipe = recipes[i];
					outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Input ingredient") + "\n", CairoFont.WhiteSmallText()));
					var element = new SlideshowItemstackTextComponent(capi, [recipe.Input.ResolvedItemStack], 40.0, EnumFloat.Inline,
						cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
					outComponents.Add(element);
					outComponents.Add(new ClearFloatTextComponent(capi));

					var steps = recipe.Steps;
					for(int j = 0; j < steps.Length; j++)
					{
						var useTime = steps[j].UseTime;
						if(useTime.HasValue)
						{
							outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0} (processing time: {1}s)", j + 1,
								useTime.Value.ToString("G", CultureInfo.InvariantCulture)) + "\n", CairoFont.WhiteSmallText()));
						}
						else
						{
							outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0}", j + 1) + "\n", CairoFont.WhiteSmallText()));
						}

						foreach(var pair in steps[j].Tools)
						{
							var descriptor = mod.GetWorkbenchToolDescriptor(pair.Key);
							if(descriptor == null)
							{
								if(toolItems.TryGetValue(pair.Key, out var list))
								{
									element = new SlideshowItemstackTextComponent(capi, list.ToArray(), 40.0, EnumFloat.Inline,
										cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
									outComponents.Add(element);
								}
							}
							else
							{
								descriptor.GetStepInfoForHandbook(capi, itemstack, recipe, j, pair.Value, openDetailPageFor, outComponents);
							}
							outComponents.Add(new ClearFloatTextComponent(capi));
						}
					}
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				}
			}
		}
	}
}