using GlassMaking.Blocks.Multiblock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.Handbook
{
	public class MultiblockPlanMaterials : IDisposable
	{
		public MultiblockPlanMaterials()
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
			if(inSlot.Itemstack.Class != EnumItemClass.Block) return;

			var block = inSlot.Itemstack.Block;
			if(block is BlockHorizontalStructurePlanMain mainBlock)
			{
				if(mainBlock.structure != null)
				{
					var structure = mainBlock.structure;
					int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
					List<ItemStack> items = new List<ItemStack>();
					for(int x = 0; x < sx; x++)
					{
						for(int y = 0; y < sy; y++)
						{
							for(int z = 0; z < sz; z++)
							{
								if(structure[x, y, z] is BlockHorizontalStructurePlan planBlock)
								{
									if(planBlock.replacement != null)
									{
										var item = (planBlock.replacement.requirement ?? planBlock.replacement.block)?.ResolvedItemstack;
										if(item != null)
										{
											int index = items.FindIndex(itm => itm.Equals(capi.World, item, GlobalConstants.IgnoredStackAttributes));
											if(index < 0) items.Add(item.Clone());
											else items[index].StackSize += item.StackSize;
										}
									}
								}
							}
						}
					}
					if(items.Count > 0)
					{
						outComponents.Add(new ClearFloatTextComponent(capi, 7f));
						outComponents.AddHandbookBoldRichText(capi, Lang.Get("glassmaking:Required materials according to plan") + "\n", openDetailPageFor);

						foreach(var item in items)
						{
							var element = new SlideshowItemstackTextComponent(capi, new ItemStack[] { item }, 40.0,
								EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)));
							element.ShowStackSize = item.StackSize > 1;
							element.PaddingRight = GuiElement.scaled(10.0);
							outComponents.Add(element);
						}

						outComponents.Add(new ClearFloatTextComponent(capi, 7f));
					}
				}
			}
		}
	}
}