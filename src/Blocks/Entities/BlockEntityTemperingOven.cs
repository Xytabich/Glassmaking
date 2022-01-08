using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
    public class BlockEntityTemperingOven : BlockEntityDisplay, ITimeBasedHeatReceiver
    {
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "glassmaking:temperingoven";
        public override string AttributeTransformCode => "temperingOvenTransform";

        protected virtual int itemCapacity => 9;

        private InventoryGeneric inventory;

        private ITimeBasedHeatSource heatSource = null;
        private ItemProcessInfo[] processes;

        private int gridSize;
        private float gridCellSize;

        private bool preventMeshUpdate = false;

        public BlockEntityTemperingOven()
        {
            gridSize = itemCapacity;
            inventory = new InventoryGeneric(itemCapacity, InventoryClassName + "-" + Pos, null);
            processes = new ItemProcessInfo[itemCapacity];
        }

        public override void Initialize(ICoreAPI api)
        {
            preventMeshUpdate = true;
            base.Initialize(api);
            preventMeshUpdate = false;
            for(int i = 0; i < processes.Length; i++)
            {
                if(processes[i] != null) ResolveProcessInfo(i);
            }
            UpdateGrid();
            updateMeshes();
        }

        void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSource heatSource)
        {
            this.heatSource = heatSource;
        }

        void ITimeBasedHeatReceiver.OnHeatSourceTick(float dt)
        {
            double totalHours = Api.World.Calendar.TotalHours;
            for(int i = 0; i < itemCapacity; i++)
            {
                var slot = inventory[i];
                var process = processes[i];
                if(!slot.Empty && process != null)
                {
                    double timeOffset = 0;
                    if(!process.isHeated)
                    {
                        var time = heatSource.CalcTempElapsedTime(0, process.temperingTemperature);
                        if(time > 0)
                        {
                            process.isHeated = true;
                            timeOffset = time;
                        }
                    }
                    if(process.isHeated)
                    {
                        process.time += (totalHours - heatSource.GetLastTickTime()) - timeOffset - heatSource.CalcTempElapsedTime(timeOffset, process.temperingTemperature);
                        if(process.time >= process.temperingTime)
                        {
                            processes[i] = null;
                            slot.Itemstack = process.output.Clone();
                            MarkDirty(true);
                        }
                    }
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            inventory.ToTreeAttributes(tree);
            for(int i = 0; i < processes.Length; i++)
            {
                if(processes[i] != null)
                {
                    var attrib = tree.GetOrAddTreeAttribute("process" + i);
                    attrib.SetBool("isHeated", processes[i].isHeated);
                    attrib.SetDouble("time", processes[i].time);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            preventMeshUpdate = true;
            base.FromTreeAttributes(tree, worldForResolving);
            preventMeshUpdate = false;
            for(int i = 0; i < processes.Length; i++)
            {
                var attrib = tree.GetTreeAttribute("process" + i);
                if(attrib != null)
                {
                    processes[i] = new ItemProcessInfo() {
                        isHeated = attrib.GetBool("isHeated"),
                        time = attrib.GetDouble("time")
                    };
                }
                else
                {
                    processes[i] = null;
                }
            }
            if(Api?.World != null)
            {
                for(int i = 0; i < processes.Length; i++)
                {
                    if(processes[i] != null) ResolveProcessInfo(i);
                }
                UpdateGrid();
                updateMeshes();
            }
        }

        public override void TranslateMesh(MeshData mesh, int index)
        {
            base.TranslateMesh(mesh, index);
        }

        public override void updateMeshes()
        {
            if(preventMeshUpdate) return;
            base.updateMeshes();
        }

        private void ResolveProcessInfo(int index)
        {
            var properties = inventory[index].Itemstack.Collectible.Attributes?["tempering"];
            if(properties != null && properties.Exists)
            {
                var stack = properties["output"].AsObject<JsonItemStack>();
                if(stack.Resolve(Api.World, "tempering oven"))
                {
                    processes[index].temperingTemperature = properties["temperature"].AsFloat();
                    processes[index].temperingTime = properties["time"].AsInt() / 3600.0;
                    processes[index].output = stack.ResolvedItemstack;
                    return;
                }
            }
            processes[index] = null;
        }

        private void UpdateGrid()
        {
            float maxSize = 0f;
            int itemsCount = 0;
            for(int i = 0; i < itemCapacity; i++)
            {
                var slot = inventory[i];
                if(!slot.Empty)
                {
                    itemsCount++;
                    maxSize = Math.Max(maxSize, slot.Itemstack.ItemAttributes?["temperingOvenSize"].AsFloat() ?? 0f);
                }
            }
            if(itemsCount > 0 && gridSize == 0)
            {
                maxSize = Math.Min(maxSize, itemsCount > 4 ? (1 / 3f) : (itemsCount > 1 ? 0.5f : 1f));
                if(maxSize <= 1f / 3f)
                {
                    gridCellSize = 1f / 3f;
                    gridSize = 3;
                }
                else if(maxSize <= 0.5f)
                {
                    gridCellSize = 0.5f;
                    gridSize = 2;
                }
                else
                {
                    gridCellSize = 1f;
                    gridSize = 1;
                }
            }
            if(itemsCount == 0) gridSize = 0;
        }

        private class ItemProcessInfo
        {
            public float temperingTemperature;
            public double temperingTime;
            public ItemStack output;
            public bool isHeated;
            public double time;
        }
    }
}