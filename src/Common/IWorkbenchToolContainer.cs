using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    public interface IWorkbenchToolContainer
    {
        Cuboidf[] GetContainerBoundingBoxes(IWorldAccessor world, ItemStack itemStack);

        WorkbenchToolInfo[] GetTools(IWorldAccessor world, ItemStack itemStack);
    }
}