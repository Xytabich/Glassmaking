﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class GlassSmelteryInventory : InventoryBase
    {
        private List<ItemSlot> slots = new List<ItemSlot>();

        public GlassSmelteryInventory(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
        }

        public GlassSmelteryInventory(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
        }

        public override int Count => slots.Count;

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

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots.Clear();
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
    }
}