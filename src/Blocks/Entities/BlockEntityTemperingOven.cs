using GlassMaking.Common;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
        private ItemStack lastRemoved = null;

        private ITimeBasedHeatSource heatSource = null;
        private ItemProcessInfo[] processes;

        private int gridSize;
        private float gridCellSize;

        private bool preventMeshUpdate = false;

        public BlockEntityTemperingOven() : base()
        {
            gridSize = itemCapacity;
            inventory = new InventoryGeneric(itemCapacity, InventoryClassName + "-" + Pos, null);
            processes = new ItemProcessInfo[itemCapacity];
            meshes = new MeshData[itemCapacity];
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
            if(Api.Side == EnumAppSide.Client) updateMeshes();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if(!inventory.Empty)
            {
                dsc.AppendLine("Contents:");
                for(int i = 0; i < itemCapacity; i++)
                {
                    if(!inventory[i].Empty)
                    {
                        dsc.Append(inventory[i].GetStackName());
                        float temperature = (inventory[i].Itemstack.Attributes["temperature"] as ITreeAttribute)?.GetFloat("temperature", 20f) ?? 20f;
                        dsc.Append(" Temperature: " + temperature.ToString("0"));
                        if(processes[i] != null && processes[i].isHeated)
                        {
                            dsc.Append(" Tempering: " + (Math.Min(processes[i].time / processes[i].temperingTime, 1) * 100).ToString("0") + "%");
                        }
                        dsc.AppendLine();
                    }
                }
            }
        }

        public bool TryInteract(IPlayer byPlayer, ItemSlot slot)
        {
            if(slot.Empty || slot.Itemstack.Equals(Api.World, lastRemoved, GlobalConstants.IgnoredStackAttributes))
            {
                bool removed = false;
                for(int i = 0; i < processes.Length; i++)
                {
                    if(!inventory[i].Empty && (processes[i] == null || byPlayer.Entity.Controls.Sneak))
                    {
                        slot.Itemstack = inventory[i].TakeOut(1);
                        lastRemoved = slot.Itemstack.Clone();
                        processes[i] = null;
                        removed = true;
                        break;
                    }
                }
                if(removed)
                {
                    if(inventory.Empty)
                    {
                        gridSize = 0;
                        lastRemoved = null;
                    }
                    if(Api.Side == EnumAppSide.Client) updateMeshes();
                    MarkDirty(true);
                    return true;
                }
                lastRemoved = null;
                return false;
            }
            else
            {
                var properties = slot.Itemstack.Collectible.Attributes?["tempering"];
                if(properties != null && properties.Exists)
                {
                    if(gridSize > 0)
                    {
                        float size = slot.Itemstack.Collectible.Attributes?["temperingOvenSize"].AsFloat(1f) ?? 1f;
                        if(size > gridCellSize) return false;

                        int len = gridSize * gridSize;
                        for(int i = 0; i < len; i++)
                        {
                            if(inventory[i].Empty)
                            {
                                inventory[i].Itemstack = slot.TakeOut(1);
                                lastRemoved = null;
                                processes[i] = new ItemProcessInfo() { isHeated = false, time = 0 };
                                ResolveProcessInfo(i);
                                if(Api.Side == EnumAppSide.Client) updateMeshes();
                                MarkDirty(true);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        float size = slot.Itemstack.Collectible.Attributes?["temperingOvenSize"].AsFloat(1f) ?? 1f;
                        if(size > 1) return false;
                        if(size <= 1f / 3f)
                        {
                            gridCellSize = 1f / 3f;
                            gridSize = 3;
                        }
                        else if(size <= 0.5f)
                        {
                            gridCellSize = 0.5f;
                            gridSize = 2;
                        }
                        else
                        {
                            gridCellSize = 1f;
                            gridSize = 1;
                        }
                        inventory[0].Itemstack = slot.TakeOut(1);
                        lastRemoved = null;
                        processes[0] = new ItemProcessInfo() { isHeated = false, time = 0 };
                        ResolveProcessInfo(0);
                        if(Api.Side == EnumAppSide.Client) updateMeshes();
                        MarkDirty(true);
                        return true;
                    }
                }
            }
            return false;
        }

        void ITimeBasedHeatReceiver.SetHeatSource(ITimeBasedHeatSource heatSource)
        {
            this.heatSource = heatSource;
        }

        void ITimeBasedHeatReceiver.OnHeatSourceTick(float dt)
        {
            if(gridSize != 0)
            {
                var totalHours = Api.World.Calendar.TotalHours;
                var graph = heatSource.CalcHeatGraph();
                for(int i = 0; i < itemCapacity; i++)
                {
                    var slot = inventory[i];
                    if(!slot.Empty)
                    {
                        float temperature = (slot.Itemstack.Attributes["temperature"] as ITreeAttribute)?.GetFloat("temperature", 20f) ?? 20f;
                        var process = processes[i];
                        if(process != null)
                        {
                            double timeOffset = 0;
                            if(!process.isHeated)
                            {
                                if(Api.Side == EnumAppSide.Server)
                                {
                                    var time = graph.CalcHeatingTime(temperature, 1000f, process.temperingTemperature.max);
                                    if(time.HasValue)
                                    {
                                        process.isHeated = true;
                                        timeOffset = time.Value;
                                        MarkDirty(true);
                                    }
                                }
                            }
                            if(process.isHeated)
                            {
                                process.time += Math.Max(0, Math.Min((temperature - process.temperingTemperature.min) / 90f, totalHours - heatSource.GetLastTickTime()) - timeOffset);
                                if(process.time >= process.temperingTime && Api.Side == EnumAppSide.Server)
                                {
                                    processes[i] = null;
                                    slot.Itemstack = process.output.Clone();
                                    MarkDirty(true);
                                }
                            }
                        }
                        temperature = graph.CalcFinalTemperature(temperature, 1000f, 90f);
                        slot.Itemstack.Collectible.SetTemperature(Api.World, slot.Itemstack, temperature);
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
                if(Api.Side == EnumAppSide.Client) updateMeshes();
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
            var stack = inventory[index].Itemstack;
            var properties = stack.Collectible.Attributes?["tempering"];
            if(properties != null && properties.Exists)
            {
                var output = properties["output"].AsObject<JsonItemStack>(null, stack.Collectible.Code.Domain);
                if(output.Resolve(Api.World, "tempering oven"))
                {
                    processes[index].temperingTemperature = properties["temperature"].AsObject<MinMaxFloat>();
                    processes[index].temperingTime = properties["time"].AsInt() / 3600.0;
                    processes[index].output = output.ResolvedItemstack;
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
                    maxSize = Math.Max(maxSize, slot.Itemstack.ItemAttributes?["temperingOvenSize"].AsFloat(1f) ?? 1f);
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
            public MinMaxFloat temperingTemperature;
            public double temperingTime;
            public ItemStack output;
            public bool isHeated;
            public double time;
        }
    }
}