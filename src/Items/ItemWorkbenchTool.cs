using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
    public class ItemWorkbenchTool : Item, IWorkbenchTool
    {
        private ContainerInfo toolContainer;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            toolContainer = Attributes?["workbenchTool"].AsObject<ContainerInfo>();
            if(toolContainer == null) api.World.Logger.Log(EnumLogType.Error, "The workbenchTool attribute was not found or is not in the correct format. Item: " + Code);
        }

        public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
        {
            return toolContainer.boundingBoxes;
        }

        [JsonObject]
        private class ContainerInfo
        {
            [JsonProperty(Required = Required.Always)]
            public Cuboidf[] boundingBoxes;
            public ToolInfo[] tools;

            [JsonObject]
            public class ToolInfo
            {
                public AssetLocation code;
                public JsonObject attributes;
            }
        }
    }
}