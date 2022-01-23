using GlassMaking.Workbench;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Blocks
{
    public class WorkbenchToolsInventory : InventoryBase
    {
        public override int Count => slots.Length;

        public FastList<int> modifiedSlots = new FastList<int>();

        protected ItemSlot[] slots;
        protected WorkbenchToolBehavior[] behaviors;
        protected BlockEntity blockentity;

        private IAttribute[] cachedAttributes;

        public WorkbenchToolsInventory(int quantitySlots, string className, string instanceID, ICoreAPI api, BlockEntity blockentity) : base(className, instanceID, api)
        {
            this.blockentity = blockentity;
            slots = GenEmptySlots(quantitySlots);
            cachedAttributes = new IAttribute[quantitySlots];
            behaviors = new WorkbenchToolBehavior[quantitySlots];
        }

        public WorkbenchToolsInventory(int quantitySlots, string inventoryID, ICoreAPI api, BlockEntity blockentity) : base(inventoryID, api)
        {
            this.blockentity = blockentity;
            slots = GenEmptySlots(quantitySlots);
            cachedAttributes = new IAttribute[quantitySlots];
            behaviors = new WorkbenchToolBehavior[quantitySlots];
        }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if(slotId < 0 || slotId >= Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                return slots[slotId];
            }
            set
            {
                if(slotId < 0 || slotId >= Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                slots[slotId] = value ?? throw new ArgumentNullException("value");
            }
        }

        public WorkbenchToolBehavior GetBehavior(int slotId)
        {
            return behaviors[slotId];
        }

        public void SetItem(int slotId, ItemStack itemStack)
        {
            if(behaviors[slotId] != null)
            {
                behaviors[slotId].OnUnloaded();
                behaviors[slotId] = null;
            }
            slots[slotId].Itemstack = itemStack;
            if(itemStack.Collectible is IWorkbenchTool tool)
            {
                behaviors[slotId] = tool.CreateToolBehavior(Api.World, itemStack, blockentity);
                behaviors[slotId].OnLoaded(Api, slots[slotId]);
            }
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            for(int i = 0; i < slots.Length; i++)
            {
                if(!slots[i].Empty && slots[i].Itemstack.Collectible is IWorkbenchTool tool)
                {
                    behaviors[i] = tool.CreateToolBehavior(Api.World, slots[i].Itemstack, blockentity);
                    behaviors[i].OnLoaded(api, slots[i]);
                    behaviors[i].FromAttribute(cachedAttributes[i], api.World);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            if(tree == null) return;

            modifiedSlots.Clear();
            var slotsAttrib = tree.GetTreeAttribute("slots");
            var toolsAttrib = tree.GetTreeAttribute("tools");
            for(int i = 0; i < slots.Length; i++)
            {
                ItemStack newstack = slotsAttrib?.GetItemstack(i.ToString());
                ItemStack oldstack = slots[i].Itemstack;

                if(Api?.World == null)
                {
                    slots[i].Itemstack = newstack;
                    cachedAttributes[i] = toolsAttrib?[i.ToString()];
                    continue;
                }

                newstack?.ResolveBlockOrItem(Api.World);

                bool isModified = (newstack != null && !newstack.Equals(Api.World, oldstack)) || (oldstack != null && !oldstack.Equals(Api.World, newstack));

                if(isModified)
                {
                    modifiedSlots.Add(i);
                    if(behaviors[i] != null)
                    {
                        behaviors[i].OnUnloaded();
                        behaviors[i] = null;
                    }
                }

                slots[i].Itemstack = newstack;

                if(isModified && newstack.Collectible is IWorkbenchTool tool)
                {
                    behaviors[i] = tool.CreateToolBehavior(Api.World, newstack, blockentity);
                    behaviors[i].OnLoaded(Api, slots[i]);
                }
                behaviors[i]?.FromAttribute(toolsAttrib?[i.ToString()], Api.World);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
            ITreeAttribute toolsAttrib = null;
            for(int i = 0; i < behaviors.Length; i++)
            {
                var attr = behaviors[i]?.ToAttribute();
                if(attr != null)
                {
                    if(toolsAttrib == null) toolsAttrib = tree.GetOrAddTreeAttribute("tools");
                    toolsAttrib[i.ToString()] = attr;
                }
            }
        }

        protected override ItemSlot NewSlot(int i)
        {
            var slot = base.NewSlot(i);
            slot.MaxSlotStackSize = 1;
            return slot;
        }
    }
}