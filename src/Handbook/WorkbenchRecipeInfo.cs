using GlassMaking.Workbench;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
	public class WorkbenchRecipeInfo : IDisposable
	{
		private GlassMakingMod mod;

		public WorkbenchRecipeInfo(GlassMakingMod mod)
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
			List<WorkbenchRecipe> recipes = null;
			foreach(var recipe in mod.GetWorkbenchRecipes())
			{
				if(recipe.Value.Output.ResolvedItemstack != null && recipe.Value.Output.ResolvedItemstack.Equals(capi.World, itemstack, GlobalConstants.IgnoredStackAttributes))
				{
					if(recipes == null) recipes = new List<WorkbenchRecipe>();
					recipes.Add(recipe.Value);
				}
			}
			if(recipes != null)
			{
				var toolItems = GetItemsByToolCode(capi);

				outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				outComponents.AddHandbookBoldRichText(capi, Lang.Get("glassmaking:Crafted at the glassmaker's workbench") + "\n", openDetailPageFor);
				for(int i = 0; i < recipes.Count; i++)
				{
					if(recipes.Count > 1)
					{
						outComponents.Add(new RichTextComponent(capi, Lang.Get("glassmaking:Steps for recipe {0}", i + 1) + "\n", CairoFont.WhiteSmallText()));
					}
					var recipe = recipes[i];
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
									var element = new SlideshowItemstackTextComponent(capi, list.ToArray(), 40.0, EnumFloat.Inline,
										cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
									outComponents.Add(element);
								}
							}
							else
							{
								descriptor.GetStepInfoForHandbook(capi, itemstack, recipe, j, openDetailPageFor, outComponents);
							}
							outComponents.Add(new ClearFloatTextComponent(capi));
						}
					}
					outComponents.Add(new ClearFloatTextComponent(capi, 7f));
				}
			}
		}

		public static IReadOnlyDictionary<string, IReadOnlyList<ItemStack>> GetItemsByToolCode(ICoreClientAPI capi)
		{
			return ObjectCacheUtil.GetOrCreate(capi, "glassmaking:workbench-toolitemsbycode", () =>
			{
				var itemsByToolCode = new Dictionary<string, IReadOnlyList<ItemStack>>();
				foreach(var obj in capi.World.Collectibles)
				{
					if(obj is IWorkbenchTool)
					{
						var list = obj.GetHandBookStacks(capi);
						if(list != null)
						{
							foreach(var item in list)
							{
								if(item.Collectible is IWorkbenchTool tool)
								{
									var code = tool.GetToolCode(capi.World, item);
									if(!itemsByToolCode.TryGetValue(code, out var items))
									{
										itemsByToolCode[code] = items = new List<ItemStack>();
									}
									((List<ItemStack>)items).Add(item);
								}
							}
						}
					}
				}
				return itemsByToolCode;
			});
		}
	}
}