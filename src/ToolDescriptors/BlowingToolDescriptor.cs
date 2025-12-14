using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.ToolDescriptors
{
	public class BlowingToolDescriptor : ToolBehaviorDescriptor<BlowingTool>
	{
		public BlowingToolDescriptor(GlassMakingMod mod) : base(mod)
		{
		}

		public override void OnLoaded(ICoreAPI api)
		{
			var tools = ToolCollection.Create(api);
			foreach(var item in api.World.BlockItemEnumerator())
			{
				foreach(var beh in item.CollectibleBehaviors)
				{
					if(IsSuitableBehavior(item, beh))
					{
						var code = ((GlassblowingToolBehavior)beh).ToolCode;
						mod.AddPipeToolDescriptor(code, this);
						tools?.AddItem(code, item);
					}
				}
			}
			handbookItemsByType = tools?.Collect()!;
		}

		public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
		{
			var step = recipe.Steps[stepIndex];
			outComponents.Add(new SlideshowItemstackTextComponent(capi, handbookItemsByType[step.Tool], 40.0, EnumFloat.Inline,
				cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
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
