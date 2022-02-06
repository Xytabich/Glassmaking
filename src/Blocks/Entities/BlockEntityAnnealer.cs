using GlassMaking.Common;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
    public class BlockEntityAnnealer : BlockEntityDisplay, ITimeBasedHeatReceiver
    {
        private static SimpleParticleProperties smokeParticles;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "glassmaking:annealer";
        public override string AttributeTransformCode => "annealerTransform";

        protected virtual int itemCapacity => 9;

        private InventoryGeneric inventory;
        private ItemStack lastRemoved = null;

        private ITimeBasedHeatSource heatSource = null;
        private ItemProcessInfo[] processes;

        private int gridSize = 0;
        private float gridCellSize;

        private bool preventMeshUpdate = false;

        public BlockEntityAnnealer()
        {
            inventory = new InventoryGeneric(itemCapacity, InventoryClassName + "-" + Pos, null);
            for(int i = itemCapacity - 1; i >= 0; i--)
            {
                inventory[i].MaxSlotStackSize = 1;
            }
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
                dsc.AppendLine(Lang.Get("Contents:"));
                for(int i = 0; i < itemCapacity; i++)
                {
                    if(!inventory[i].Empty)
                    {
                        dsc.Append(inventory[i].GetStackName());
                        float temperature = (inventory[i].Itemstack.Attributes["temperature"] as ITreeAttribute)?.GetFloat("temperature", 20f) ?? 20f;
                        dsc.Append(' ').Append(Lang.Get("Temperature: {0}°C", temperature.ToString("0")));
                        if(processes[i] != null && processes[i].isHeated)
                        {
                            dsc.Append(' ').Append(Lang.Get("glassmaking:Annealing: {0}", (Math.Min(processes[i].time / processes[i].annealTime, 1) * 100).ToString("0")));
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
                        inventory[i].TryPutInto(Api.World, slot, 1);
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
                var properties = slot.Itemstack.Collectible.Attributes?["glassmaking:anneal"];
                if(properties != null && properties.Exists)
                {
                    if(gridSize > 0)
                    {
                        float size = slot.Itemstack.Collectible.Attributes?["annealerSize"].AsFloat(1f) ?? 1f;
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
                        float size = slot.Itemstack.Collectible.Attributes?["annealerSize"].AsFloat(1f) ?? 1f;
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
                                    var time = graph.CalcHeatingTime(temperature, 1000f, process.annealTemperature.max);
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
                                process.time += Math.Max(0, Math.Min((temperature - process.annealTemperature.min) / 90f, totalHours - heatSource.GetLastTickTime()) - timeOffset);
                                if(process.time >= process.annealTime && Api.Side == EnumAppSide.Server)
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

            if(Api.Side == EnumAppSide.Client)
            {
                if(heatSource.IsBurning()) EmitParticles();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            inventory.ToTreeAttributes(tree);
            for(int i = 0; i < processes.Length; i++)
            {
                if(!inventory[i].Empty && processes[i] != null)
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
            int x = index % gridSize;
            int z = index / gridSize;
            var transform = ((BlockAnnealer)Block).contentTransform;
            mesh.Translate(transform.Translation.X + (x + 0.5f) / gridSize * transform.ScaleXYZ.X, transform.Translation.Y, transform.Translation.Z + (z + 0.5f) / gridSize * transform.ScaleXYZ.Z);
        }

        public override void updateMeshes()
        {
            if(preventMeshUpdate) return;
            base.updateMeshes();
        }

        private void ResolveProcessInfo(int index)
        {
            var stack = inventory[index].Itemstack;
            var properties = stack.Collectible.Attributes?["glassmaking:anneal"];
            if(properties != null && properties.Exists)
            {
                var output = properties["output"].AsObject<JsonItemStack>(null, stack.Collectible.Code.Domain);
                if(output.Resolve(Api.World, "annealer"))
                {
                    processes[index].annealTemperature = properties["temperature"].AsObject<MinMaxFloat>();
                    processes[index].annealTime = properties["time"].AsInt() / 3600.0;
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
                    maxSize = Math.Max(maxSize, slot.Itemstack.ItemAttributes?["annealerSize"].AsFloat(1f) ?? 1f);
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

        private void EmitParticles()
        {
            if(Api.World.Rand.Next(5) > 0)
            {
                var transform = ((BlockAnnealer)Block).smokeTransform;
                smokeParticles.MinPos.Set(Pos.X + transform.Translation.X, Pos.Y + transform.Translation.Y, Pos.Z + transform.Translation.Z);
                smokeParticles.AddPos.Set(transform.ScaleXYZ.X, 0.0, transform.ScaleXYZ.Z);
                Api.World.SpawnParticles(smokeParticles);
            }
        }

        static BlockEntityAnnealer()
        {
            smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(128, 110, 110, 110), new Vec3d(), new Vec3d(), new Vec3f(-0.2f, 0.3f, -0.2f), new Vec3f(0.2f, 0.3f, 0.2f), 2f, 0f, 0.5f, 1f, EnumParticleModel.Quad);
            smokeParticles.SelfPropelled = true;
            smokeParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255f);
            smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 2f);
        }

        private class ItemProcessInfo
        {
            public MinMaxFloat annealTemperature;
            public double annealTime;
            public ItemStack output;
            public bool isHeated;
            public double time;
        }
    }
}