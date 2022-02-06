using GlassMaking.GlassblowingTools;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.ToolDescriptors
{
    public abstract class ToolBehaviorDescriptor<T> : ToolBehaviorDescriptor, IPipeBlowingToolDescriptor where T : GlassblowingToolBehavior
    {
        protected GlassMakingMod mod;

        protected Dictionary<string, ItemStack[]> itemsByType;

        public ToolBehaviorDescriptor(GlassMakingMod mod)
        {
            this.mod = mod;
        }

        public override void OnLoaded(ICoreClientAPI capi)
        {
            var byType = new Dictionary<string, List<ItemStack>>();
            foreach(var item in capi.World.Collectibles)
            {
                foreach(var beh in item.CollectibleBehaviors)
                {
                    if(IsSuitableBehavior(item, beh))
                    {
                        var code = ((GlassblowingToolBehavior)beh).toolCode;
                        if(!byType.TryGetValue(code, out var list))
                        {
                            byType[code] = list = new List<ItemStack>();
                        }
                        mod.AddPipeToolDescriptor(code, this);
                        List<ItemStack> stacks = item.GetHandBookStacks(capi);
                        if(stacks != null) list.AddRange(stacks);
                    }
                }
            }
            itemsByType = new Dictionary<string, ItemStack[]>(byType.Count);
            foreach(var pair in byType)
            {
                itemsByType[pair.Key] = pair.Value.ToArray();
            }
        }

        public override void OnUnloaded()
        {

        }

        public abstract void GetStepInfoForHandbook(ICoreClientAPI capi, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, ActionConsumable<string> openDetailPageFor, List<RichTextComponentBase> outComponents);

        public abstract void GetStepInfoForHeldItem(IWorldAccessor world, ItemStack item, GlassBlowingRecipe recipe, int stepIndex, StringBuilder dsc, bool withDebugInfo);

        protected virtual bool IsSuitableBehavior(CollectibleObject item, CollectibleBehavior beh)
        {
            return beh.GetType() == typeof(T);
        }
    }

    public abstract class ToolBehaviorDescriptor
    {
        public abstract void OnLoaded(ICoreClientAPI capi);

        public abstract void OnUnloaded();
    }
}