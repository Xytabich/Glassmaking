using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GlassMaking.ToolDescriptors
{
    public class IntakeToolDescriptor : ToolBehaviorDescriptor<GlassIntakeTool>
    {
        private ItemStack[] items;

        public IntakeToolDescriptor(GlassMakingMod mod) : base(mod)
        {
        }

        public override void OnLoaded(ICoreClientAPI capi)
        {
            List<ItemStack> list = new List<ItemStack>();
            foreach(var item in capi.World.Collectibles)
            {
                foreach(var beh in item.CollectibleBehaviors)
                {
                    if(IsSuitableBehavior(item, beh))
                    {
                        mod.AddPipeToolDescriptor(((GlassblowingToolBehavior)beh).toolCode, this);
                    }
                }
            }
            foreach(var block in capi.World.Blocks)
            {
                if(block is BlockGlassSmeltery)
                {
                    List<ItemStack> stacks = block.GetHandBookStacks(capi);
                    if(stacks != null) list.AddRange(stacks);
                }
            }
            items = list.ToArray();
        }

        public override void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents)
        {
            var step = recipe.steps[stepIndex];
            outComponents.Add(new RichTextComponent(capi, "• " + Lang.Get("glassmaking:Step {0}: {1}", stepIndex + 1,
                Lang.Get("glassmaking:Take {0} units of {1} glass", step.attributes["amount"].AsInt(),
                Lang.Get(GlassBlend.GetBlendNameCode(new AssetLocation(step.attributes["code"].AsString()))))) + "\n", CairoFont.WhiteSmallText()));

            outComponents.Add(new SlideshowItemstackTextComponent(capi, items, 40.0, EnumFloat.Inline,
                cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
            outComponents.Add(new ClearFloatTextComponent(capi));
        }

        public override void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo)
        {
            var step = recipe.steps[stepIndex];
            dsc.AppendLine("• " + Lang.Get("glassmaking:Take {0} units of {1} glass", step.attributes["amount"].AsInt(),
                Lang.Get(GlassBlend.GetBlendNameCode(new AssetLocation(step.attributes["code"].AsString())))));
        }
    }
}