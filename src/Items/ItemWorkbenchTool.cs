using GlassMaking.Workbench;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
    public abstract class ItemWorkbenchTool : Item, IWorkbenchTool
    {
        private ContainerInfo toolContainer;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            toolContainer = Attributes?["workbenchTool"].AsObject<ContainerInfo>();
            if(toolContainer == null) api.World.Logger.Log(EnumLogType.Error, "The workbenchTool attribute was not found or is not in the correct format. Item: " + Code);
        }

        public virtual AssetLocation GetToolCode(IWorldAccessor world, ItemStack itemStack)
        {
            return toolContainer.code;
        }

        public virtual JsonObject GetToolAttributes(IWorldAccessor world, ItemStack itemStack)
        {
            return toolContainer.attributes;
        }

        public virtual Cuboidf[] GetContainerBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
        {
            return toolContainer.boundingBoxes;
        }

        public abstract WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);

        [JsonObject]
        private class ContainerInfo
        {
            [JsonProperty(Required = Required.Always)]
            public AssetLocation code;
            public JsonObject attributes;
            [JsonProperty(Required = Required.Always)]
            public Cuboidf[] boundingBoxes;
        }
    }
}