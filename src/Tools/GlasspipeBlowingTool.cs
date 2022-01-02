using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Tools
{
    public class GlasspipeBlowingTool : IGlassBlowingTool
    {
        public GlassBlowingToolStep GetStepInstance()
        {
            return new ToolStep();
        }

        private class ToolStep : GlassBlowingToolStep
        {
            public override GlassBlowingToolStep Clone()
            {
                return new ToolStep() {
                    tool = tool,
                    shape = shape == null ? null : (int[,])shape.Clone()
                };
            }

            public override WorldInteraction[] GetHeldInteractionHelp(ITreeAttribute treeAttribute)
            {
                return new WorldInteraction[0];
            }

            public override bool Resolve(JsonObject attributes, IWorldAccessor world, string sourceForErrorLogging)
            {
                return true;
            }
        }
    }
}