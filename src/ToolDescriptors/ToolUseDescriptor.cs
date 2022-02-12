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
    public class ToolUseDescriptor : ToolBehaviorDescriptor<ToolUse>
    {
        public ToolUseDescriptor(GlassMakingMod mod) : base(mod)
        {
        }

        public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
        {
            var step = recipe.steps[stepIndex];
            outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0}: {1}", stepIndex + 1, Lang.Get("glassmaking:{0} for {1} seconds",
                Lang.Get("glassmaking:glassblowingtool-" + step.tool), step.attributes["time"].AsFloat(1).ToString("G", CultureInfo.InvariantCulture))) + "\n", CairoFont.WhiteSmallText()));
            outComponents.Add(new SlideshowItemstackTextComponent(capi, handbookItemsByType[step.tool], 40.0, EnumFloat.Inline,
                cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
            outComponents.Add(new ClearFloatTextComponent(capi));
        }

        public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
        {
            var step = recipe.steps[stepIndex];
            dsc.AppendLine("• " + Lang.Get("glassmaking:{0} for {1} seconds", Lang.Get("glassmaking:glassblowingtool-" + step.tool), step.attributes["time"].AsFloat(1).ToString("G", CultureInfo.InvariantCulture)));
        }

        protected override bool IsSuitableBehavior(CollectibleObject item, CollectibleBehavior beh)
        {
            if(base.IsSuitableBehavior(item, beh))
            {
                return item.ToolTier >= ((ToolUse)beh).minTier;
            }
            return false;
        }
    }
}