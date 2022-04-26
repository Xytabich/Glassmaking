using GlassMaking.Blocks.Renderer;
using GlassMaking.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class LatheToolBehavior : WorkbenchToolBehavior
	{
		public const string CODE = "lathe";

		private AnimatorBase animator;
		private BlockAnimatableRenderer renderer;
		private LatheAnimatorUpdater updater;

		public LatheToolBehavior(BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(CODE, blockentity, boundingBoxes)
		{
		}

		public override void OnLoaded(ICoreAPI api, ItemSlot slot)
		{
			base.OnLoaded(api, slot);
			if(api.Side == EnumAppSide.Client)
			{
				var capi = (ICoreClientAPI)api;
				AnimUtil.GetAnimatableMesh(capi, slot.Itemstack.Collectible, new AtlasTexSource(capi, capi.BlockTextureAtlas), out var meshRef, out var shape);
				animator = BlockEntityAnimationUtil.GetAnimator(api, "glassmaking:lathe|" + slot.Itemstack.Collectible.Code.ToString(), shape);

				updater = new LatheAnimatorUpdater(animator);
				capi.Event.RegisterRenderer(updater, EnumRenderStage.Opaque, "glassmaking:lathe");

				ModelTransform transform = slot.Itemstack.Collectible.Attributes?["workbenchToolTransform"].AsObject<ModelTransform>().EnsureDefaultValues();
				renderer = new BlockAnimatableRenderer(capi, Blockentity.Pos, new Vec3f(0, Blockentity.Block.Shape.rotateY, 0), transform, animator, meshRef, false);
			}
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

		private void Dispose()
		{
			if(Api is ICoreClientAPI capi)
			{
				renderer?.Dispose();
				capi.Event.UnregisterRenderer(updater, EnumRenderStage.Opaque);
			}
		}

		private class LatheAnimatorUpdater : IRenderer
		{
			public double RenderOrder => 1.0;

			public int RenderRange => 99;

			private AnimatorBase animator;

			private RunningAnimation viseState = null;
			private float viseFrame;

			private Dictionary<string, AnimationMetaData> activeAnimationsByAnimCode = new Dictionary<string, AnimationMetaData>();

			public LatheAnimatorUpdater(AnimatorBase animator)
			{
				this.animator = animator;
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

			public void SetVise(float? value)
			{
				if(value.HasValue)
				{
					if(!activeAnimationsByAnimCode.ContainsKey("vise"))
					{
						activeAnimationsByAnimCode["vise"] = new AnimationMetaData() { AnimationSpeed = 0f, BlendMode = EnumAnimationBlendMode.Add };
					}
					var state = animator.GetAnimationState("vise");
					viseFrame = (1f - value.Value) * (state.Animation.QuantityFrames - 1);
					viseState = animator.GetAnimationState("vise");
				}
				else
				{
					activeAnimationsByAnimCode.Remove("vise");
					viseState = null;
				}
			}

			public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
			{
				if(activeAnimationsByAnimCode.Count > 0 || animator.ActiveAnimationCount > 0)
				{
					animator.OnFrame(activeAnimationsByAnimCode, deltaTime);
					if(viseState != null)
					{
						viseState.Iterations = 0;
						viseState.EasingFactor = Math.Min(1f, viseState.EasingFactor + (1f - viseState.EasingFactor) * deltaTime * viseState.meta.EaseInSpeed);
						viseState.CurrentFrame = viseFrame;
					}
				}
			}

			public void Dispose() { }
		}
	}
}