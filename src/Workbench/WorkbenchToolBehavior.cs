using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Workbench
{
    public abstract class WorkbenchToolBehavior
    {
        public AssetLocation code;

        /// <summary>
        /// The block for this behavior instance.
        /// </summary>
        public BlockEntity Blockentity;

        /// <summary>
        /// The properties of this block behavior.
        /// </summary>
        public JsonObject properties;

        public ICoreAPI Api;

        public WorkbenchToolBehavior(BlockEntity blockentity)
        {
            this.Blockentity = blockentity;
        }

        /// <summary>
        /// Called right after the block behavior was created
        /// </summary>
        /// <param name="properties"></param>
        public virtual void OnLoaded(ICoreAPI api, JsonObject properties)
        {
            this.Api = api;
        }

        public virtual void OnUnloaded()
        {

        }

        public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return false;
        }

        public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
        }

        public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return false;
        }

        public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return false;
        }

        public virtual void FromAttribute(IAttribute attribute, IWorldAccessor worldForResolving)
        {

        }

        public virtual IAttribute ToAttribute()
        {
            return null;
        }

        public virtual void OnBlockRemoved()
        {
        }

        public virtual void OnBlockUnloaded()
        {
        }
    }
}