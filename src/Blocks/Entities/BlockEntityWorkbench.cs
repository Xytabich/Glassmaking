using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Blocks
{
    public class BlockEntityWorkbench : BlockEntityDisplay
    {
        protected virtual int itemCapacity => 9;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "glassmaking:workbench";
        public override string AttributeTransformCode => "workbenchTransform";

        private InventoryGeneric inventory;
        //private List<ModuleInfo> modules = new List<ModuleInfo>();
        //private Dictionary<string, JToken> tools = new Dictionary<string, JToken>();

        public BlockEntityWorkbench()
        {
            inventory = new InventoryGeneric(itemCapacity, InventoryClassName + "-" + Pos, null);
            meshes = new MeshData[itemCapacity];
        }

        public bool OnUseItem(IPlayer byPlayer, ItemSlot slot)
        {
            if(slot.Itemstack.Class != EnumItemClass.Block) return false;

            var attribs = slot.Itemstack.ItemAttributes;//TODO: if(stack.collectible is workbenchtool || behavior is workbenchtool)
            if(attribs == null || !attribs.KeyExists("workbenchTool")) return false;

            for(int i = 0; i < itemCapacity; i++)
            {
                if(inventory[i].Empty)
                {
                    inventory[i].Itemstack = slot.TakeOut(1);
                    slot.MarkDirty();

                    updateMesh(i);
                    MarkDirty(true);
                    return true;
                }
            }
            return false;
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

        private struct ModuleInfo
        {
            public ItemStack item;
            public MeshData mesh;

            public ModuleInfo(ItemStack item, MeshData mesh)
            {
                this.item = item;
                this.mesh = mesh;
            }
        }
    }
}