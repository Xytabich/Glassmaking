using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
	public static class Utils
	{
		public static void Write(this BinaryWriter writer, AssetLocation location)
		{
			writer.Write(location.ToShortString());
		}

		public static AssetLocation ReadAssetLocation(this BinaryReader reader)
		{
			return new AssetLocation(reader.ReadString());
		}

		/// <summary>
		/// Interpolates a bezier curve. In this case the incoming list will be reduced to one element, which will contain the result of the calculation.
		/// </summary>
		public static void InterpolateBezier(FastList<FastVec2f> points, float t)
		{
			while(points.Count > 1)
			{
				int count = points.Count - 1;
				for(int i = 0; i < count; i++)
				{
					points[i] = FastVec2f.Lerp(points[i], points[i + 1], t);
				}
				points.RemoveAt(count);
			}
		}

		public static bool Intersects(this Cuboidf self, Cuboidf other)
		{
			return self.X2 > other.X1 && self.X1 < other.X2 && self.Y2 > other.Y1 && self.Y1 < other.Y2 && self.Z2 > other.Z1 && self.Z1 < other.Z2;
		}

		public static bool IntersectsOrTouches(this Cuboidf self, Cuboidf other)
		{
			return self.X2 >= other.X1 && self.X1 <= other.X2 && self.Y2 >= other.Y1 && self.Y1 <= other.Y2 && self.Z2 >= other.Z1 && self.Z1 <= other.Z2;
		}

		public static void FixIdMappingOrClear(ItemSlot itemSlot, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, IWorldAccessor worldForNewMappings)
		{
			if(itemSlot.Itemstack != null)
			{
				if(!itemSlot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForNewMappings))
				{
					itemSlot.Itemstack = null;
				}
				else
				{
					itemSlot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForNewMappings, itemSlot, oldBlockIdMapping, oldItemIdMapping);
				}
			}
		}

		public static void FixIdMappingOrClear(ref ItemStack itemStack, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, IWorldAccessor worldForNewMappings)
		{
			if(itemStack != null)
			{
				if(!itemStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForNewMappings))
				{
					itemStack = null;
				}
				else
				{
					itemStack.Collectible.OnLoadCollectibleMappings(worldForNewMappings, new DummySlot(itemStack), oldBlockIdMapping, oldItemIdMapping);
				}
			}
		}

		public static void StoreCollectibleMappings(ItemSlot itemSlot, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping, IWorldAccessor world)
		{
			if(itemSlot.Itemstack != null)
			{
				if(itemSlot.Itemstack.Class == EnumItemClass.Item)
				{
					itemIdMapping[itemSlot.Itemstack.Item.Id] = itemSlot.Itemstack.Item.Code;
				}
				else
				{
					blockIdMapping[itemSlot.Itemstack.Block.BlockId] = itemSlot.Itemstack.Block.Code;
				}
				itemSlot.Itemstack.Collectible.OnStoreCollectibleMappings(world, itemSlot, blockIdMapping, itemIdMapping);
			}
		}

		public static Vec3f Lerp(this Vec3f self, Vec3f target, float t)
		{
			self.X += (target.X - self.X) * t;
			self.Y += (target.Y - self.Y) * t;
			self.Z += (target.Z - self.Z) * t;
			return self;
		}

		public static Vec3f LerpDelta(this Vec3f self, Vec3f delta, float t)
		{
			self.X += delta.X * t;
			self.Y += delta.Y * t;
			self.Z += delta.Z * t;
			return self;
		}

		public static ModelTransform Lerp(this ModelTransform self, ModelTransform target, float t)
		{
			self.Origin = self.Origin.Lerp(target.Origin, t);
			self.Translation = self.Translation.Lerp(target.Translation, t);
			self.Rotation = self.Rotation.Lerp(target.Rotation, t);
			self.ScaleXYZ = self.ScaleXYZ.Lerp(target.ScaleXYZ, t);
			return self;
		}

		public static ModelTransform LerpDelta(this ModelTransform self, ModelTransform delta, float t)
		{
			self.Origin = self.Origin.LerpDelta(delta.Origin, t);
			self.Translation = self.Translation.LerpDelta(delta.Translation, t);
			self.Rotation = self.Rotation.LerpDelta(delta.Rotation, t);
			self.ScaleXYZ = self.ScaleXYZ.LerpDelta(delta.ScaleXYZ, t);
			return self;
		}
	}
}