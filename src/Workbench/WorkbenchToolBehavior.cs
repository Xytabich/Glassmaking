using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
    public abstract class WorkbenchToolBehavior
    {
        public string toolCode;

        /// <summary>
        /// The block for this behavior instance.
        /// </summary>
        public BlockEntity Blockentity;

        public ICoreAPI Api;

        public ItemSlot slot;

        protected Cuboidf[] boundingBoxes;

        public WorkbenchToolBehavior(string toolCode, BlockEntity blockentity, Cuboidf[] boundingBoxes)
        {
            this.toolCode = toolCode.ToLowerInvariant();
            this.Blockentity = blockentity;
            this.boundingBoxes = boundingBoxes;
        }

        /// <summary>
        /// Called right after the tool behavior was created
        /// </summary>
        public virtual void OnLoaded(ICoreAPI api, ItemSlot slot)
        {
            this.Api = api;
            this.slot = slot;
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

        public virtual WorldInteraction[] GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return null;
        }

        public virtual ITreeAttribute ToAttribute()
        {
            return null;
        }

        public virtual void FromAttribute(IAttribute tree, IWorldAccessor worldAccessForResolve)
        {
        }

        public virtual Cuboidf[] GetBoundingBoxes()
        {
            return boundingBoxes;
        }

        public virtual void OnBlockRemoved()
        {
        }

        public virtual void OnBlockUnloaded()
        {
        }
    }
}