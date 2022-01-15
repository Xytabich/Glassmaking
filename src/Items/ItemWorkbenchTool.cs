using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
    public class ItemWorkbenchTool : Item, IWorkbenchToolContainer
    {
        private ContainerInfo toolContainer;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            toolContainer = Attributes?["workbenchTool"].AsObject<ContainerInfo>();
            if(toolContainer == null) api.World.Logger.Log(EnumLogType.Error, "The workbenchTool attribute was not found or is not in the correct format. Item: " + Code);
        }

        public virtual Cuboidf[] GetContainerBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
        {
            return toolContainer.boundingBoxes;
        }

        public virtual WorkbenchToolInfo[] GetTools(IWorldAccessor world, ItemStack itemStack)
        {
            return toolContainer.tools ?? new WorkbenchToolInfo[0];
        }

        [JsonObject]
        private class ContainerInfo
        {
            [JsonProperty(Required = Required.Always)]
            public Cuboidf[] boundingBoxes;
            public WorkbenchToolInfo[] tools;
        }
    }
}