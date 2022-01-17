using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
    public interface IWorkbenchTool
    {
        string GetToolCode(IWorldAccessor world, ItemStack itemStack);

        Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack);

        WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
    }
}