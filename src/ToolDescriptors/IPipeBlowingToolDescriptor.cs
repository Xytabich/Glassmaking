using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.ToolDescriptors
{
    public interface IPipeBlowingToolDescriptor
    {
        void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents);

        void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo);

        bool TryGetWorkingTemperature(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int currentStepIndex, out float temperature);

        void GetBreakDrops(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int currentStepIndex, List<ItemStack> outList);
    }
}