using GlassMaking.GlassblowingTools;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking.ToolDescriptors
{
	public class BlowingToolDescriptor : ToolBehaviorDescriptor<BlowingTool>
	{
		public BlowingToolDescriptor(GlassMakingMod mod) : base(mod)
		{
		}

		public override void OnLoaded(ICoreAPI api)
		{
			foreach(var item in api.World.Collectibles)
			{
				foreach(var beh in item.CollectibleBehaviors)
				{
					if(IsSuitableBehavior(item, beh))
					{
						mod.AddPipeToolDescriptor(((GlassblowingToolBehavior)beh).toolCode, this);
					}
				}
			}
		}

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var step = recipe.steps[stepIndex];
			outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0}: {1}", stepIndex + 1, Lang.Get("glassmaking:{0} for {1} seconds",
				Lang.Get("glassmaking:glassblowingtool-" + step.tool), step.attributes["time"].AsFloat(1).ToString("G", CultureInfo.InvariantCulture))) + "\n", CairoFont.WhiteSmallText()));
			outComponents.Add(new ClearFloatTextComponent(capi));
		}

		public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
		{
			var step = recipe.steps[stepIndex];
			dsc.AppendLine("• " + Lang.Get("glassmaking:{0} for {1} seconds", Lang.Get("glassmaking:glassblowingtool-" + step.tool),
				step.attributes["time"].AsFloat(1).ToString("G", CultureInfo.InvariantCulture)));
		}
	}
}
