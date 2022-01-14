using GlassMaking.Common;
using GlassMaking.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
    public class BlockGlassSmeltery : HeatedBlockBase
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if(api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:blockhelp-smeltery", () => {
                List<ItemStack> blends = new List<ItemStack>();

                foreach(Item item in api.World.Items)
                {
                    if(item is ItemGlassBlend && item.Attributes?.KeyExists(GlassBlend.PROPERTY_NAME) == true)
                    {
                        List<ItemStack> stacks = item.GetHandBookStacks(capi);
                        if(stacks != null) blends.AddRange(stacks);
                    }
                }
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "glassmaking:blockhelp-smeltery-add",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = blends.ToArray(),
                        GetMatchingStacks = GetMatchingBlends
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "glassmaking:blockhelp-smeltery-add",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = blends.ConvertAll(s => { s = s.Clone(); s.StackSize = 5; return s; }).ToArray(),
                        GetMatchingStacks = GetMatchingBlends
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "glassmaking:blockhelp-smeltery-add",
                        HotKeyCodes = new string[] { "sneak", "sprint" },
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = blends.ConvertAll(s => { s = s.Clone(); s.StackSize = 20; return s; }).ToArray(),
                        GetMatchingStacks = GetMatchingBlends
                    }
                };
            });
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            if(itemstack != null)
            {
                var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGlassSmeltery;
                if(be != null && be.CanInteract(byPlayer.Entity, blockSel))
                {
                    if(be.TryAdd(byPlayer, slot, byPlayer.Entity.Controls.Sneak ? (byPlayer.Entity.Controls.Sprint ? 20 : 5) : 1))
                    {
                        if(world.Side == EnumAppSide.Client)
                        {
                            (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var items = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if(items == null) items = new ItemStack[0];
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityGlassSmeltery;
            if(be != null) items = items.Append(be.GetDropItems() ?? new ItemStack[0]);
            return items;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            if(api.Side == EnumAppSide.Client)
            {
                var bathMesh = ObjectCacheUtil.TryGet<MeshRef>(api, "glassmaking:smeltery-shape-" + Variant["side"]);
                if(bathMesh != null)
                {
                    bathMesh.Dispose();
                    ObjectCacheUtil.Delete(api, "glassmaking:smeltery-shape" + Variant["side"]);
                }
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        private ItemStack[] GetMatchingBlends(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
        {
            if(wi.Itemstacks.Length == 0) return null;
            var be = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BlockEntityGlassSmeltery;
            if(be == null) return null;
            be.GetGlassFillState(out var canAddAmount, out var code);
            if(code == null) return wi.Itemstacks;
            if(canAddAmount <= 0) return null;
            List<ItemStack> list = new List<ItemStack>();
            foreach(var stack in wi.Itemstacks)
            {
                var blend = GlassBlend.FromJson(stack);
                if(blend.code.Equals(code) && blend.amount * stack.StackSize <= canAddAmount)
                {
                    list.Add(stack);
                }
            }
            return list.ToArray();
        }
    }
}