using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    public interface IWorkbenchTool
    {
        Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack);
    }
}