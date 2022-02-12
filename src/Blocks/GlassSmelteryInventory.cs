using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class GlassSmelteryInventory : InventoryBase
    {
        public override int Count => slots.Count;

        private List<ItemSlot> slots = new List<ItemSlot>();

        public GlassSmelteryInventory(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
        }

        public GlassSmelteryInventory(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
        }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if(slotId < 0 || slotId >= Count)
                {
                    return null;
                }
                return slots[slotId];
            }
            set
            {
                if(slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException("slotId");
                if(value == null) throw new ArgumentNullException("value");
                slots[slotId] = value;
            }
        }

        public void AddItem(ItemStack itemStack)
        {
            var dummy = new DummySlot(itemStack);
            ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, itemStack.StackSize);
            for(int i = 0; i < slots.Count; i++)
            {
                dummy.TryPutInto(slots[i], ref op);
            }
            if(dummy.StackSize > 0)
            {
                slots.Add(NewSlot(slots.Count));
                slots[slots.Count - 1].Itemstack = dummy.Itemstack.Clone();
            }
        }

        public new void Clear()
        {
            slots.Clear();
        }

        public List<ItemStack> GetItems()
        {
            return slots.ConvertAll(s => s.Itemstack);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots.Clear();
            if(tree == null) return;

            int count = tree.GetInt("qslots", 0);
            var treeSlots = tree.GetTreeAttribute("slots");
            if(treeSlots != null && count > 0)
            {
                if(slots.Capacity < count) slots.Capacity = count;
                for(int i = 0; i < count; i++)
                {
                    slots.Add(NewSlot(i));
                    var item = treeSlots.GetItemstack(i.ToString());
                    if(item != null && Api?.World != null)
                    {
                        item.ResolveBlockOrItem(Api.World);
                    }
                    slots[i].Itemstack = item;
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            int counter = 0;
            TreeAttribute treeAttribute = new TreeAttribute();
            for(int i = 0; i < slots.Count; i++)
            {
                if(slots[i].Itemstack != null)
                {
                    treeAttribute.SetItemstack(counter.ToString(), slots[i].Itemstack.Clone());
                    counter++;
                }
            }
            tree.SetInt("qslots", counter);
            tree["slots"] = treeAttribute;
        }

        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            return null;
        }

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return null;
        }

        protected override ItemSlot NewSlot(int i)
        {
            var slot = base.NewSlot(i);
            slot.MaxSlotStackSize = int.MaxValue;
            return slot;
        }
    }
}