﻿using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench
{
	/// <summary>
	/// Describes the behavior of a tool that is "embedded" to all workbenches.
	/// Is a singleton, i.e. the instance is used for all workbenches at the same time.
	/// </summary>
	public abstract class WorkbenchToolBehavior
	{
		public ICoreAPI Api = default!;

		public abstract string ToolCode { get; }

		public virtual void OnLoaded(ICoreAPI api)
		{
			Api = api;
		}

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
		/// Called when a recipe step has been completed.
		/// Here it is possible to consume items used in crafting, etc.
		/// </summary>
		public virtual void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
		}

		/// <summary>
		/// Called when a craft was cancelled.
		/// Can be invoked even if <see cref="OnUseStart"/> has not been called before.
		/// </summary>
		public virtual void OnUseCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
		}

		/// <summary>
		/// Returns hints for using this tool.
		/// This method will be called on a "embedded" tool, as long as it is part of a recipe.
		/// </summary>
		public virtual WorldInteraction[]? GetBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, WorkbenchRecipe? recipe = null, int step = 0)
		{
			return null;
		}

		/// <summary>
		/// Called if the block was removed or unloaded while the current step of the recipe used (i.e. was in the tools list) this tool.
		/// </summary>
		public virtual void OnBlockUnloadedAtStep(IWorldAccessor world, BlockPos pos, WorkbenchRecipe recipe, int step)
		{
		}
	}
}