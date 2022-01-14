using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
    public class BlockEntityWorkbench : BlockEntity
    {
        private object modulesLocker = new object();
        private List<ModuleInfo> modules = new List<ModuleInfo>();
        private Dictionary<string, JToken> tools = new Dictionary<string, JToken>();

        public bool OnUseItem(IPlayer byPlayer, ItemSlot slot)
        {
            if(slot.Itemstack.Class != EnumItemClass.Block) return false;

            var attribs = slot.Itemstack.ItemAttributes;//TODO: if(stack.collectible is workbenchtool)
            if(attribs == null || !attribs.KeyExists("workbenchTool")) return false;

            var tableModule = attribs["workbenchTool"];
            var tools = tableModule["tools"].Token as JObject;
            foreach(var tool in tools)
            {
                if(this.tools.ContainsKey(tool.Key))
                {
                    return false;
                }
            }
            foreach(var tool in tools)
            {
                this.tools.Add(tool.Key, tool.Value);
            }

            MeshData mesh = null;
            if(byPlayer.Entity.World.Api is ICoreClientAPI capi)
            {
                var block = slot.Itemstack.Block;
                var shape = block.Shape;
                capi.Tesselator.TesselateShape(block, capi.Assets.Get<Shape>(shape.Base.CopyWithPath("shapes/" + shape.Base.Path + ".json")), out mesh);
                if(tableModule.KeyExists("transform"))
                {
                    mesh.ModelTransform(tableModule["transform"].AsObject<ModelTransform>());
                }
                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, (float)(GetRotation() * (Math.PI / 180)), 0f);
            }

            var stack = slot.Itemstack.Clone();
            stack.StackSize = 1;
            lock(modulesLocker)
            {
                modules.Add(new ModuleInfo(stack, mesh));
            }
            slot.Itemstack.StackSize--;
            if(slot.Itemstack.StackSize <= 0) slot.Itemstack = null;
            slot.MarkDirty();
            MarkDirty(true, byPlayer);
            return true;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if(modules.Count == 0) return false;
            lock(modulesLocker)
            {
                foreach(var module in modules)
                {
                    mesher.AddMeshData(module.mesh);
                }
            }
            return true;
        }
        private int GetRotation()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            switch(block.LastCodePart())
            {
                case "north": return 0;
                case "east": return 270;
                case "south": return 180;
                case "west": return 90;
            }
            return 0;
        }

        //private void GetOffsetAndRotation(out int rotation, out Vec3f offset)
        //{
        //    Block block = Api.World.BlockAccessor.GetBlock(Pos);
        //    switch(block.LastCodePart())
        //    {
        //        case "east":
        //            rotation = 270;
        //            offset = new Vec3f(0, 0, -1);
        //            break;
        //        case "south":
        //            rotation = 180;
        //            offset = new Vec3f(1, 0, 0);
        //            break;
        //        case "west":
        //            rotation = 90;
        //            offset = new Vec3f(0, 0, 1);
        //            break;
        //        default:
        //            rotation = 0;
        //            offset = new Vec3f(-1, 0, 0);
        //            break;
        //    }
        //}

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