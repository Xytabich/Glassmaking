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
						mod.AddPipeToolDescriptor(((GlassblowingToolBehavior)beh).ToolCode, this);
					}
				}
			}
		}

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var step = recipe.Steps[stepIndex];
			outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0}: {1}", stepIndex + 1, Lang.Get("glassmaking:{0} for {1} seconds",
				Lang.Get("glassmaking:glassblowingtool-" + step.Tool), step.Attributes!["time"].AsFloat(1).ToString("G", CultureInfo.InvariantCulture))) + "\n", CairoFont.WhiteSmallText()));
			outComponents.Add(new ClearFloatTextComponent(capi));
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
				HotKeyCode = "sneak"
			});
		}
	}
}
