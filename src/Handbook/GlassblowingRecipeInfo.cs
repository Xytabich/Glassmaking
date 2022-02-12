using Cairo;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.Handbook
{
	public class GlassblowingRecipeInfo : IDisposable
	{
		private GlassMakingMod mod;

		public GlassblowingRecipeInfo(GlassMakingMod mod)
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
			var itemstack = inSlot.Itemstack;
			List<GlassBlowingRecipe> recipes = null;
			foreach(var recipe in mod.GetGlassBlowingRecipes())
			{
				if(recipe.Value.output.ResolvedItemstack != null && recipe.Value.output.ResolvedItemstack.Equals(capi.World, itemstack, GlobalConstants.IgnoredStackAttributes))
				{
					if(recipes == null) recipes = new List<GlassBlowingRecipe>();
					recipes.Add(recipe.Value);
				}
			}
			if(recipes != null)
			{
				outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Blown via pipe") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));
				for(int i = 0; i < recipes.Count; i++)
				{
					if(recipes.Count > 1)
					{
						outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Steps for recipe {0}", i + 1) + "\n", CairoFont.WhiteSmallText()));
					}
					var recipe = recipes[i];
					var steps = recipe.steps;
					for(int j = 0; j < steps.Length; j++)
					{
						var descriptor = mod.GetPipeToolDescriptor(steps[j].tool);
						if(descriptor == null)
						{
							outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0} tool: {1}", j + 1,
								Lang.Get("glassmaking:glassblowingtool-" + steps[j].tool)) + "\n", CairoFont.WhiteSmallText()));
						}
						else
						{
							descriptor.GetStepInfoForHandbook(capi, itemstack, recipe, j, openDetailPageFor, outComponents);
						}
					}
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				}
			}
		}
	}
}