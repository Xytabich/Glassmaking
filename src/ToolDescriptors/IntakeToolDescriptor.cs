using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using System;
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
        private HashSet<string> toolCodes = new HashSet<string>();
        private ItemStack[] items;

        public IntakeToolDescriptor(GlassMakingMod mod) : base(mod)
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
                        var code = ((GlassblowingToolBehavior)beh).toolCode;
                        toolCodes.Add(code);
                        mod.AddPipeToolDescriptor(code, this);
                    }
                }
            }
            if(api.Side == EnumAppSide.Client)
            {
                var capi = (ICoreClientAPI)api;
                List<ItemStack> list = new List<ItemStack>();
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

        public override bool TryGetWorkingTemperature(IWorldAccessor world, ItemStack itemStack, GlassBlowingRecipe recipe, int currentStepIndex, out float temperature)
        {
            var steps = recipe.steps;
            int lastIndex = currentStepIndex - 1;
            if(toolCodes.Contains(steps[currentStepIndex].tool) && itemStack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0) > 0)
            {
                lastIndex++;
            }

            temperature = 0f;
            for(int i = 0; i <= lastIndex; i++)
            {
                if(toolCodes.Contains(steps[i].tool))
                {
                    var info = mod.GetGlassTypeInfo(new AssetLocation(steps[i].attributes["code"].AsString()));
                    temperature = Math.Max(info.meltingPoint * 0.8f, temperature);
                }
            }
            return temperature > 0f;
        }

        public override void GetBreakDrops(IWorldAccessor world, ItemStack itemStack, GlassBlowingRecipe recipe, int currentStepIndex, List<ItemStack> outList)
        {
            var steps = recipe.steps;
            var amountByCode = new Dictionary<string, int>();
            if(toolCodes.Contains(steps[currentStepIndex].tool))
            {
                int intake = itemStack.Attributes.GetInt("glassmaking:toolIntakeAmount", 0);
                if(intake > 0)
                {
                    amountByCode[steps[currentStepIndex].attributes["code"].AsString()] = intake;
                }
            }

            for(int i = 0; i < currentStepIndex; i++)
            {
                if(toolCodes.Contains(steps[i].tool))
                {
                    var code = steps[i].attributes["code"].AsString();
                    if(!amountByCode.TryGetValue(code, out var amount)) amount = 0;
                    amountByCode[code] = amount + steps[i].attributes["amount"].AsInt();
                }
            }
            if(amountByCode.Count == 0) return;

            var shardsItem = world.GetItem(new AssetLocation("glassmaking", "glassshards"));
            foreach(var pair in amountByCode)
            {
                int count = pair.Value / 5;
                if(count > 0)
                {
                    var item = new ItemStack(shardsItem, count);
                    new GlassBlend(new AssetLocation(pair.Key), 5).ToTreeAttributes(item.Attributes.GetOrAddTreeAttribute(GlassBlend.PROPERTY_NAME));
                    outList.Add(item);
                }
            }
        }
    }
}