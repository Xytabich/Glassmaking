using GlassMaking.GlassblowingTools;
using GlassMaking.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.ToolDescriptors
{
	public class DryableToolDescriptor : ToolBehaviorDescriptor<DryableTool>
	{
		public DryableToolDescriptor(GlassMakingMod mod) : base(mod)
		{
		}

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var step = recipe.Steps[stepIndex];
			outComponents.Add(new SlideshowItemstackTextComponent(capi, handbookItemsByType[step.Tool], 40.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));

			var consume = step.Attributes?["consume"]?.AsFloat(0);
			if(consume > 0)
			{
				var waterItem = capi.World.GetItem(ItemWettable.WaterCode);
				var waterProps = waterItem?.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
				if(waterProps != null)
				{
					var stackSize = (int)Math.Ceiling(waterProps.ItemsPerLitre * consume.Value);
					if(stackSize < 1) stackSize = 1;

					outComponents.Add(new ItemstackTextComponent(capi, new ItemStack(waterItem, stackSize), 40.0, 0.0, EnumFloat.Inline,
						cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))) {
						ShowStacksize = true
					});
				}
			}
		}

		public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
		{
			var step = recipe.Steps[stepIndex];
			dsc.AppendLine("• " + Lang.Get("glassmaking:{0} for {1} seconds", Lang.Get("glassmaking:glassblowingtool-" + step.Tool),
				step.Attributes!["time"].AsFloat(1).ToString("G", CultureInfo.InvariantCulture)));
		}

		public override void GetInteractionHelp(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, List<WorldInteraction> interactions)
		{
			var step = recipe.Steps[stepIndex];
			interactions.Add(new WorldInteraction() {
				ActionLangCode = "glassmaking:glassblowingtool-" + step.Tool,
				MouseButton = EnumMouseButton.Right,
				Itemstacks = handbookItemsByType[step.Tool]
			});
		}
	}
}