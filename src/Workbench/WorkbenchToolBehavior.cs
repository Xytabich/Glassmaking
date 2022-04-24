using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	public abstract class WorkbenchToolBehavior
	{
		public string ToolCode;

		/// <summary>
		/// The block for this behavior instance.
		/// </summary>
		public BlockEntity Blockentity;

		public ICoreAPI Api;

		public ItemSlot Slot;

		protected Cuboidf[] boundingBoxes;

		public WorkbenchToolBehavior(string toolCode, BlockEntity blockentity, Cuboidf[] boundingBoxes)
		{
			ToolCode = toolCode.ToLowerInvariant();
			Blockentity = blockentity;
			this.boundingBoxes = boundingBoxes;
		}

		/// <summary>
		/// Called right after the tool behavior was created
		/// </summary>
		public virtual void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			Api = api;
			Slot = slot;
		}

		/// <summary>
		/// Called when a tool has been removed from a workbench
		/// </summary>
		public virtual void OnUnloaded()
		{

		}

		/// <summary>
		/// Called when the use of a tool in a recipe begins.
		/// </summary>
		/// <returns>False if the use should be canceled</returns>
		public virtual bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			return true;
		}

		/// <summary>
		/// Called every frame while the player is crafting using this tool.
		/// </summary>
		/// <returns>False if the use should be canceled</returns>
		public virtual bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			return true;
		}

		/// <summary>
		/// Called when a recipe step has been completed. Here it is possible to consume items used in crafting, etc.
		/// </summary>
		public virtual void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
		}

		/// <summary>
		/// Called when a craft was cancelled. But at the same time, <see cref="OnUseStart"/> may not be called on this behavior.
		/// </summary>
		public virtual void OnUseCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
		}

		public virtual WorldInteraction[] GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, WorkbenchRecipe recipe = null, int step = 0)
		{
			return null;
		}

		public virtual ITreeAttribute ToAttribute()
		{
			return null;
		}

		public virtual void FromAttribute(IAttribute attribute, IWorldAccessor worldAccessForResolve)
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

		public virtual bool TryGetWorkpieceTransform(out ModelTransform recipeTransform)
		{
			recipeTransform = null;
			return false;
		}
	}
}