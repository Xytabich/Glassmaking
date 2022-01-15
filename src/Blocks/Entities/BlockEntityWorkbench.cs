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

        private ToolItemInfo[] itemsInfo;
        private InventoryGeneric inventory;

        private Cuboidf[] selectionBoxes;

        public BlockEntityWorkbench()
        {
            inventory = new InventoryGeneric(itemCapacity, InventoryClassName + "-" + Pos, null);
            meshes = new MeshData[itemCapacity];
            itemsInfo = new ToolItemInfo[itemCapacity];
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            InitTools();
        }

        public bool OnUseItem(IPlayer byPlayer, ItemSlot slot)
        {
            if(!(slot.Itemstack.Collectible is IWorkbenchTool tool)) return false;
            var world = byPlayer.Entity.World;
            var boxes = GetRotatedBoxes(tool.GetToolBoundingBoxes(world, slot.Itemstack), Block.Shape.rotateY);
            for(int i = itemCapacity - 1; i >= 0; i--)
            {
                if(itemsInfo[i] != null)
                {
                    if(HasIntersections(boxes, itemsInfo[i].boundingBoxes))
                    {
                        return false;
                    }
                }
            }

            for(int i = 0; i < itemCapacity; i++)
            {
                if(inventory[i].Empty)
                {
                    inventory[i].Itemstack = slot.TakeOut(1);
                    itemsInfo[i] = new ToolItemInfo() { boundingBoxes = boxes };
                    slot.MarkDirty();

                    RebuildSelectionBoxes();

                    updateMesh(i);
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
            if(Api?.World != null) InitTools();
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

        private void InitTools()
        {
            for(int i = itemCapacity - 1; i >= 0; i--)
            {
                var slot = inventory[i];
                if(slot.Empty || !(slot.Itemstack.Collectible is IWorkbenchTool tool))
                {
                    itemsInfo[i] = null;
                }
                else
                {
                    var boxes = GetRotatedBoxes(tool.GetToolBoundingBoxes(Api.World, slot.Itemstack), Block.Shape.rotateY);
                    itemsInfo[i] = new ToolItemInfo() { boundingBoxes = boxes };
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
                if(itemsInfo[i] != null)
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
            target.X1 = verts[0];
            target.Y1 = verts[1];
            target.Z1 = verts[2];
            target.X2 = verts[3];
            target.Y2 = verts[4];
            target.Z2 = verts[5];
        }

        private static bool HasIntersections(Cuboidf[] a, Cuboidf[] b)
        {
            for(int i = a.Length - 1; i >= 0; i--)
            {
                for(int j = b.Length - 1; j >= 0; j--)
                {
                    if(a[i].Equals(b[i]) || a[i].Intersects(b[j])) return true;
                }
            }
            return false;
        }

        private class ToolItemInfo
        {
            public int selIndex;
            public Cuboidf[] boundingBoxes;
        }
    }
}