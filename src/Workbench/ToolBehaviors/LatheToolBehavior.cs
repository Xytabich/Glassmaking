using GlassMaking.Blocks;
using GlassMaking.Blocks.Renderer;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class LatheToolBehavior : WorkbenchMountedToolBehavior
	{
		public const string CODE = "lathe";

		private AnimatorBase animator = default!;
		private BlockAnimatableRenderer renderer = default!;
		private LatheAnimatorUpdater updater = default!;

		private float[] localTransform = default!;

		private readonly IWorkbenchRenderInfo workbenchRender;

		public LatheToolBehavior(BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(CODE, blockentity, boundingBoxes)
		{
			workbenchRender = (blockentity as IWorkbenchRenderInfo)!;
			if(workbenchRender == null) throw new NullReferenceException("Blockentity must implement IWorkbenchRenderInfo");
		}

		public override void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api, slot);
			if(api.Side == EnumAppSide.Client)
			{
				var capi = (ICoreClientAPI)api;
				AnimUtil.GetAnimatableMesh(capi, slot.Itemstack.Collectible, new AtlasTexSource(capi, capi.BlockTextureAtlas), out var meshRef, out var shape);
				animator = AnimationUtil.GetAnimator(api, "glassmaking:lathe|" + slot.Itemstack.Collectible.Code.ToString(), shape);

				updater = new LatheAnimatorUpdater(animator);
				capi.Event.RegisterRenderer(updater, EnumRenderStage.Opaque, "glassmaking:lathe");

				var transform = slot.Itemstack.Collectible.Attributes?["workbenchToolTransform"].AsObject<ModelTransform>().EnsureDefaultValues();
				renderer = new BlockAnimatableRenderer(capi, Blockentity.Pos, new Vec3f(0, Blockentity.Block.Shape.rotateY, 0), transform, animator, meshRef, false);

				(transform ?? ModelTransform.NoTransform).CopyTo(updater.LocalTransform);
				localTransform = updater.LocalTransform;
			}
		}

		public override void OnIdleStart(IWorldAccessor world, WorkbenchRecipe recipe, int step)
		{
			SetupRenderer(recipe.Steps[step].Tools[CODE]!);
		}

		public override void OnIdleStop(IWorldAccessor world, WorkbenchRecipe? recipe, int step)
		{
			updater.SetJawSpacing(null);

			updater.TargetItemTransform = null;
		}

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client)
			{
				var useTime = recipe.Steps[step].UseTime;
				if(useTime.HasValue)
				{
					var latheInfo = recipe.Steps[step].Tools[CODE];
					updater.SetRotationSpeed(latheInfo!["rpm"].AsFloat(15));
					SetupRenderer(latheInfo);
				}
			}
			return base.OnUseStart(world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client)
			{
				updater.SetRotationSpeed(null);
				updater.SetJawSpacing(null);

				updater.TargetItemTransform = null;
			}
			base.OnUseComplete(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(Api.Side == EnumAppSide.Client)
			{
				updater.SetRotationSpeed(null);
				updater.SetJawSpacing(null);

				updater.TargetItemTransform = null;
			}
			base.OnUseCancel(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUnloaded()
		{
			base.OnUnloaded();
			Dispose();
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			Dispose();
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();
			Dispose();
		}

		private void SetupRenderer(JsonObject latheInfo)
		{
			updater.SetJawSpacing(GameMath.Clamp(latheInfo["size"].AsFloat(0.1f) / Slot.Itemstack.Collectible.Attributes["latheChuckMaxSize"]?.AsFloat(0.1f) ?? 0.1f, 0f, 1f));
			if(latheInfo.KeyExists("transform"))
			{
				var mat = latheInfo["transform"].AsObject<ModelTransform>().EnsureDefaultValues();
				updater.SetItemTransform(mat);

				updater.TargetItemTransform = workbenchRender.WorkpieceRenderer.ItemTransform.Values;
				updater.ForceUpdate();
			}
		}

		private void Dispose()
		{
			if(Api is ICoreClientAPI capi)
			{
				renderer?.Dispose();
				capi.Event.UnregisterRenderer(updater, EnumRenderStage.Opaque);
			}
		}

		private class LatheAnimatorUpdater : IRenderer//TODO: animations not working anymore
		{
			public double RenderOrder => 1.0;

			public int RenderRange => 99;

			public float[]? TargetItemTransform = null;
			public readonly float[] LocalTransform = Mat4f.Create();

			private AnimatorBase animator;

			private float? jawSpacing = null;
			private float? prevJawSpacing = null;
			private RunningAnimation? jawsState = null;
			private float jawsFrame;

			private readonly float[] itemTransform = Mat4f.Create();
			private readonly AttachmentPointAndPose itemAttachmentPoint;

			private readonly Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode = new();

			public LatheAnimatorUpdater(AnimatorBase animator)
			{
				this.animator = animator;
				itemAttachmentPoint = animator.AttachmentPointByCode["item"];
			}

			public void ForceUpdate()
			{
				OnRenderFrame(0f, EnumRenderStage.Opaque);
			}

			public void SetItemTransform(ModelTransform mat)
			{
				mat.CopyTo(itemTransform);

				var attachTransform = Mat4f.Create();
				var attachPoint = itemAttachmentPoint.AttachPoint;
				Mat4f.Translate(attachTransform, attachTransform, (float)attachPoint.PosX / 16f, (float)attachPoint.PosY / 16f, (float)attachPoint.PosZ / 16f);
				Mat4f.RotateX(attachTransform, attachTransform, (float)(attachPoint.RotationX * GameMath.DEG2RAD));
				Mat4f.RotateY(attachTransform, attachTransform, (float)(attachPoint.RotationY * GameMath.DEG2RAD));
				Mat4f.RotateZ(attachTransform, attachTransform, (float)(attachPoint.RotationZ * GameMath.DEG2RAD));

				Mat4f.Mul(itemTransform, attachTransform, itemTransform);
			}

			public void SetRotationSpeed(float? rpm)
			{
				if(rpm.HasValue)
				{
					if(!activeAnimationsByAnimCode.TryGetValue("rotation", out var anim))
					{
						activeAnimationsByAnimCode["rotation"] = anim = new AnimationMetaData() { BlendMode = EnumAnimationBlendMode.Add };
					}
					var state = animator.GetAnimationState("rotation");
					anim.AnimationSpeed = rpm.Value * state.Animation.QuantityFrames * (1f / 1800f);
				}
				else
				{
					activeAnimationsByAnimCode.Remove("rotation");
				}
			}

			public void SetJawSpacing(float? value)
			{
				this.jawSpacing = value;
			}

			public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
			{
				if(jawSpacing != prevJawSpacing)
				{
					prevJawSpacing = jawSpacing;
					if(jawSpacing.HasValue)
					{
						if(!activeAnimationsByAnimCode.ContainsKey("jaws"))
						{
							activeAnimationsByAnimCode["jaws"] = new AnimationMetaData() { AnimationSpeed = 0f, BlendMode = EnumAnimationBlendMode.Add };
						}
						var state = animator.GetAnimationState("jaws");
						jawsFrame = (1f - jawSpacing.Value) * (state.Animation.QuantityFrames - 1);
						jawsState = animator.GetAnimationState("jaws");
						jawsState.EasingFactor = 0;
					}
					else
					{
						activeAnimationsByAnimCode["jaws"].AnimationSpeed = 1f;
						activeAnimationsByAnimCode.Remove("jaws");
						jawsState = null;
					}
				}
				if(activeAnimationsByAnimCode.Count > 0 || animator.ActiveAnimationCount > 0)
				{
					animator.OnFrame(activeAnimationsByAnimCode, deltaTime);
					if(jawsState != null)
					{
						jawsState.Iterations = 0;
						jawsState.EasingFactor = Math.Min(1f, jawsState.EasingFactor + (1f - jawsState.EasingFactor) * deltaTime * jawsState.meta.EaseInSpeed);
						jawsState.CurrentFrame = jawsFrame;
					}
					if(TargetItemTransform != null)
					{
						itemTransform.CopyTo(TargetItemTransform, 0);

						Mat4f.Mul(TargetItemTransform, itemAttachmentPoint.AnimModelMatrix, TargetItemTransform);

						Mat4f.Mul(TargetItemTransform, LocalTransform, TargetItemTransform);
					}
				}
			}

			public void Dispose() { }
		}
	}
}