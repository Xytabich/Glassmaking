using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
    public interface IWorkbenchTool
    {
        AssetLocation GetToolCode(IWorldAccessor world, ItemStack itemStack);

        JsonObject GetToolAttributes(IWorldAccessor world, ItemStack itemStack);

        Cuboidf[] GetContainerBoundingBoxes(IWorldAccessor world, ItemStack itemStack);

        WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
    }
}