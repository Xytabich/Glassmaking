using GlassMaking.Workbench;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
    public class BlockEntityWorkbench : BlockEntityDisplay
    {
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "glassmaking:workbench";
        public override string AttributeTransformCode => "workbenchTransform";

        protected virtual int itemCapacity => 9;

        private ContainerInfo[] itemsInfo;
        private InventoryGeneric inventory;

        private Cuboidf[] selectionBoxes;
        private Dictionary<string, WorkbenchToolBehavior> tools = new Dictionary<string, WorkbenchToolBehavior>();

        public BlockEntityWorkbench()
        {
            inventory = new InventoryGeneric(itemCapacity, InventoryClassName + "-" + Pos, null);
            meshes = new MeshData[itemCapacity];
            itemsInfo = new ContainerInfo[itemCapacity];
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            UpdateTools();
        }

        public bool OnUseItem(IPlayer byPlayer, ItemSlot slot)
        {
            if(!(slot.Itemstack.Collectible is IWorkbenchTool tool)) return false;
            var world = byPlayer.Entity.World;
            var boxes = GetRotatedBoxes(tool.GetContainerBoundingBoxes(world, slot.Itemstack), Block.Shape.rotateY);
            for(int i = itemCapacity - 1; i >= 0; i--)
            {
                if(itemsInfo[i] != null && !itemsInfo[i].Empty)
                {
                    if(HasIntersections(boxes, itemsInfo[i].boundingBoxes))
                    {
                        return false;
                    }
                }
            }

            var toolCode = tool.GetToolCode(world, slot.Itemstack).ToShortString();
            if(tools.ContainsKey(toolCode))
            {
                return false;
            }

            for(int i = 0; i < itemCapacity; i++)
            {
                if(inventory[i].Empty)
                {
                    inventory[i].Itemstack = slot.TakeOut(1);

                    var behavior = tool.CreateToolBehavior(world, slot.Itemstack, this);
                    var attribs = tool.GetToolAttributes(world, slot.Itemstack);
                    itemsInfo[i] = new ContainerInfo() { boundingBoxes = boxes, tool = behavior, attributes = attribs };
                    tools.Add(toolCode, behavior);
                    behavior.OnLoaded(Api, attribs);

                    RebuildSelectionBoxes();
                    updateMesh(i);

                    slot.MarkDirty();
                    MarkDirty(true);
                    return true;
                }
            }

            return false;
        }

        public Cuboidf[] GetSelectionBoxes()
        {
            return selectionBoxes;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            for(int i = 0; i < itemCapacity; i++)
            {
                var data = tree["tooldata" + i];
                if(itemsInfo[i] == null)
                {
                    if(data != null)
                    {
                        itemsInfo[i] = new ContainerInfo() { data = data };
                    }
                }
                else
                {
                    itemsInfo[i].data = data;
                }
            }
            if(Api?.World != null) UpdateTools();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            for(int i = 0; i < itemCapacity; i++)
            {
                if(itemsInfo[i] != null)
                {
                    var attrib = itemsInfo[i].Empty ? itemsInfo[i].data : itemsInfo[i].tool.ToAttribute();
                    if(attrib != null) tree["tooldata" + i] = attrib;
                }
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            foreach(var tool in tools)
            {
                tool.Value.OnBlockRemoved();
            }
            tools.Clear();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            foreach(var tool in tools)
            {
                tool.Value.OnBlockUnloaded();
            }
            tools.Clear();
        }

        protected override MeshData genMesh(ItemStack stack)
        {
            MeshData mesh;
            var dynBlock = stack.Collectible as IContainedMeshSource;

            if(dynBlock != null)
            {
                mesh = dynBlock.GenMesh(stack, capi.BlockTextureAtlas, Pos);
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, Block.Shape.rotateY * GameMath.DEG2RAD, 0);
            }
            else
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if(stack.Class == EnumItemClass.Block)
                {
                    mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                }
                else
                {
                    nowTesselatingObj = stack.Collectible;
                    nowTesselatingShape = null;
                    if(stack.Item.Shape != null)
                    {
                        nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                    }
                    capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

                    mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
                }
            }

            if(stack.Collectible.Attributes?[AttributeTransformCode].Exists == true)
            {
                ModelTransform transform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);

                mesh.Rotate(new Vec3f(0.5f, 0f, 0.5f), 0f, Block.Shape.rotateY * GameMath.DEG2RAD, 0f);
            }

            if(stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), GameMath.PIHALF, 0, 0);
                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.5f, 0.33f);
                mesh.Translate(0, -7.5f / 16f, 0f);
            }

            return mesh;
        }

        private void UpdateTools()
        {
            tools.Clear();
            for(int i = itemCapacity - 1; i >= 0; i--)
            {
                var slot = inventory[i];
                var info = itemsInfo[i];
                if(slot.Empty || !(slot.Itemstack.Collectible is IWorkbenchTool tool))
                {
                    if(info != null && !itemsInfo[i].Empty) info.tool.OnUnloaded();
                    itemsInfo[i] = null;
                }
                else
                {
                    var attribs = tool.GetToolAttributes(Api.World, slot.Itemstack);
                    var code = tool.GetToolCode(Api.World, slot.Itemstack).ToShortString();
                    IAttribute data = info?.PickData();
                    if(info != null && code == info.tool.code.ToShortString() && (attribs == null && info.attributes == null || JToken.DeepEquals(attribs?.Token, info.attributes?.Token)))
                    {
                        tools[code] = info.tool;
                        info.tool.FromAttribute(data, Api.World);
                        continue;
                    }
                    else
                    {
                        if(info != null && !itemsInfo[i].Empty) info.tool.OnUnloaded();

                        var behavior = tool.CreateToolBehavior(Api.World, slot.Itemstack, this);
                        tools[code] = behavior;
                        var boxes = GetRotatedBoxes(tool.GetContainerBoundingBoxes(Api.World, slot.Itemstack), Block.Shape.rotateY);
                        itemsInfo[i] = new ContainerInfo() { boundingBoxes = boxes, tool = behavior, attributes = attribs };
                        behavior.OnLoaded(Api, attribs);
                        if(data != null) behavior.FromAttribute(data, Api.World);
                    }
                }
            }
            RebuildSelectionBoxes();
        }

        private void RebuildSelectionBoxes()
        {
            var boxes = new List<Cuboidf>();
            if(Block.SelectionBoxes != null) boxes.AddRange(Block.SelectionBoxes);
            else boxes.Add(Block.DefaultCollisionBox);
            for(int i = itemCapacity - 1; i >= 0; i--)
            {
                if(itemsInfo[i] != null && !itemsInfo[i].Empty)
                {
                    itemsInfo[i].selIndex = boxes.Count;
                    boxes.AddRange(itemsInfo[i].boundingBoxes);
                }
            }
            selectionBoxes = boxes.ToArray();
        }

        private static Cuboidf[] GetRotatedBoxes(Cuboidf[] source, float rotation)
        {
            if(rotation == 0f) return source;
            var boxes = new Cuboidf[source.Length];
            float[] verts = new float[6];
            float[] mat = Mat4f.Create();
            Mat4f.RotateY(mat, mat, rotation * GameMath.DEG2RAD);
            var origin = new Vec3f(0.5f, 0f, 0.5f);
            for(int i = source.Length - 1; i >= 0; i--)
            {
                FillVerts(verts, source[i]);
                Mat4f.MulWithVec3_Position_WithOrigin(mat, verts, verts, 0, origin);
                Mat4f.MulWithVec3_Position_WithOrigin(mat, verts, verts, 3, origin);
                FillCuboid(verts, boxes[i] = new Cuboidf());
            }
            return boxes;
        }

        private static void FillVerts(float[] verts, Cuboidf source)
        {
            verts[0] = source.X1;
            verts[1] = source.Y1;
            verts[2] = source.Z1;
            verts[3] = source.X2;
            verts[4] = source.Y2;
            verts[5] = source.Z2;
        }

        private static void FillCuboid(float[] verts, Cuboidf target)
        {
            target.X1 = Math.Min(verts[0], verts[3]);
            target.Y1 = Math.Min(verts[1], verts[4]);
            target.Z1 = Math.Min(verts[2], verts[5]);
            target.X2 = Math.Max(verts[0], verts[3]);
            target.Y2 = Math.Max(verts[1], verts[4]);
            target.Z2 = Math.Max(verts[2], verts[5]);
        }

        private static bool HasIntersections(Cuboidf[] a, Cuboidf[] b)
        {
            for(int i = a.Length - 1; i >= 0; i--)
            {
                for(int j = b.Length - 1; j >= 0; j--)
                {
                    if(a[i].Intersects(b[j])) return true;
                }
            }
            return false;
        }

        private class ContainerInfo
        {
            public int selIndex;
            public Cuboidf[] boundingBoxes;
            public JsonObject attributes;
            public WorkbenchToolBehavior tool = null;
            public IAttribute data;

            public bool Empty { get { return tool == null; } }

            public IAttribute PickData()
            {
                var data = this.data;
                this.data = null;
                return data;
            }
        }
    }
}