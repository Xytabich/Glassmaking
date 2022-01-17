using GlassMaking.Workbench;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
    public abstract class ItemWorkbenchTool : Item, IWorkbenchTool
    {
        protected Cuboidf[] toolBoundingBoxes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            toolBoundingBoxes = Attributes?["workbenchToolBounds"].AsObject<Cuboidf[]>();
        }

        public abstract string GetToolCode(IWorldAccessor world, ItemStack itemStack);

        public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
        {
            return toolBoundingBoxes;
        }

        public abstract WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
    }
}