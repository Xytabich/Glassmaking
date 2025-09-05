using GlassMaking.Common;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking
{
	public static class AnimUtil
	{
		public static float Reach(float current, float to)
		{
			return Math.Sign(to) * Math.Min(Math.Abs(to), current);
		}

		public static float Reach(float current, float from, float to)
		{
			return from + Reach(current, to - from);
		}

		public static float Tri(float startValue, float midValue, float endValue, float midTime, float t)
		{
			if(t < midTime) return startValue + t / midTime * (midValue - startValue);
			return midValue + (t - midTime) / (1 - midTime) * (endValue - midValue);
		}

		public static float Tri(float midPoint, float t)
		{
			if(t < midPoint) return t / midPoint;
			return (t - midPoint) / (1 - midPoint);
		}

		public static float Quad(float p1, float p2, float p3, float p4, float p2Time, float p3Time, float t)
		{
			if(t < p2Time) return p1 + t / p2Time * (p2 - p1);
			if(t < p3Time) return p2 + (t - p2Time) / (p3Time - p2Time) * (p3 - p2);
			return p3 + (t - p3Time) / (1 - p3Time) * (p4 - p3);
		}

		/// <summary>
		/// Returns a reference to a mesh and shape that can be used for animation.
		/// Should only be called in main thread.
		/// </summary>
		public static void GetAnimatableMesh(ICoreClientAPI capi, CollectibleObject collectible, AtlasTexSource texSource, out MultiTextureMeshRef meshRef, out Shape shape)
		{
			var cache = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:animmeshcache", () => new Dictionary<string, MeshInfo>());
			var cacheKey = collectible.Code.ToString();

			if(!cache.TryGetValue(cacheKey, out var meshInfo))
			{
				var collShape = collectible.ItemClass == EnumItemClass.Item ? ((Item)collectible).Shape : ((Block)collectible).Shape;
				shape = capi.TesselatorManager.GetCachedShape(collShape.Base);

				shape.ResolveReferences(capi.World.Logger, cacheKey);
				CacheInvTransforms(shape.Elements);
				shape.ResolveAndFindJoints(capi.Logger, cacheKey);
				texSource.Init(collectible, shape);
				capi.Tesselator.TesselateShape(new TesselationMetaData {
					TexSource = texSource,
					WithJointIds = true,
					WithDamageEffect = true,
					TypeForLogging = "blockanim",
					Rotation = Vec3f.Zero,
					QuantityElements = collShape.QuantityElements,
					SelectiveElements = collShape.SelectiveElements,
				}, shape, out var modeldata);
				meshRef = capi.Render.UploadMultiTextureMesh(modeldata);

				meshInfo = new MeshInfo() { shape = shape, meshRef = meshRef };
				cache[cacheKey] = meshInfo;
			}
			meshRef = meshInfo.meshRef;
			shape = meshInfo.shape;
		}

		public static void CacheInvTransforms(ShapeElement[] elements)
		{
			if(elements != null)
			{
				for(int i = 0; i < elements.Length; i++)
				{
					elements[i].CacheInverseTransformMatrix();
					CacheInvTransforms(elements[i].Children);
				}
			}
		}

		internal static void ReleaseResources(ICoreClientAPI capi)
		{
			var meshCache = ObjectCacheUtil.TryGet<Dictionary<string, MeshInfo>>(capi, "glassmaking:animmeshcache");
			if(meshCache != null)
			{
				ObjectCacheUtil.Delete(capi, "glassmaking:animmeshcache");
				foreach(var pair in meshCache)
				{
					pair.Value.meshRef?.Dispose();
				}
			}
		}

		private struct MeshInfo
		{
			public MultiTextureMeshRef meshRef;
			public Shape shape;
		}
	}
}