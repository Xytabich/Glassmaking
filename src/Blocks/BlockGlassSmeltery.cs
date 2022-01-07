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

            interactions = ObjectCacheUtil.GetOrCreate(api, "glassmaking:blockhelp-glasssmeltery", () => {
                List<ItemStack> blends = new List<ItemStack>();

                foreach(CollectibleObject obj in api.World.Collectibles)
                {
                    if(obj is ItemGlassBlend || obj.Attributes?.KeyExists(GlassBlend.PROPERTY_NAME) == true)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                        if(stacks != null) blends.AddRange(stacks);
                    }
                }
                //TODO: check amount & bubbling reducer & filter by current content
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "glassmaking:blockhelp-glasssmeltery-add",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = blends.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "glassmaking:blockhelp-glasssmeltery-add",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = blends.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "glassmaking:blockhelp-glasssmeltery-add",
                        HotKeyCodes = new string[] { "sneak", "sprint" },
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = blends.ToArray()
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
                var bathMesh = ObjectCacheUtil.TryGet<MeshRef>(api, "glassmaking:glass-smeltery-shape");
                if(bathMesh != null)
                {
                    bathMesh.Dispose();
                    ObjectCacheUtil.Delete(api, "glassmaking:glass-smeltery-shape");
                }
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}