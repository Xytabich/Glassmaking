using Cairo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
	public static class Utils
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this BinaryWriter writer, AssetLocation location)
		{
			writer.Write(location.ToShortString());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AssetLocation ReadAssetLocation(this BinaryReader reader)
		{
			return new AssetLocation(reader.ReadString());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(this BinaryWriter writer, Vec3f value)
		{
			writer.Write(value.X);
			writer.Write(value.Y);
			writer.Write(value.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3f ReadVec3f(this BinaryReader reader)
		{
			float x = reader.ReadSingle();
			float y = reader.ReadSingle();
			float z = reader.ReadSingle();
			return new Vec3f(x, y, z);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(this Cuboidf self, Cuboidf other)
		{
			return self.X2 > other.X1 && self.X1 < other.X2 && self.Y2 > other.Y1 && self.Y1 < other.Y2 && self.Z2 > other.Z1 && self.Z1 < other.Z2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AddHandbookBoldRichText(this List<RichTextComponentBase> components, ICoreClientAPI capi, string text, ActionConsumable<string> openDetailPageFor = null)
		{
			components.AddRange(VtmlUtil.Richtextify(capi, text, CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), r => openDetailPageFor?.Invoke(r.Href)));
		}

		public static void CopyFrom(this BlockSelection self, BlockSelection other)
		{
			self.Position.Set(other.Position);
			self.HitPosition.Set(other.HitPosition);
			self.Face = other.Face;
			self.SelectionBoxIndex = other.SelectionBoxIndex;
			self.DidOffset = other.DidOffset;
		}

		public static void CopyTo(this ModelTransform self, ModelTransform other)
		{
			other.Translation.Set(self.Translation);
			other.Rotation.Set(self.Rotation);
			other.ScaleXYZ.Set(self.ScaleXYZ);
			other.Origin.Set(self.Origin);
			other.Rotate = self.Rotate;
		}

		public static void CopyTo(this ModelTransform self, Matrixf other)
		{
			CopyTo(self, other.Values);
		}

		public static void CopyTo(this ModelTransform self, float[] other)
		{
			Mat4f.Identity(other);
			Mat4f.Translate(other, other, self.Translation.X, self.Translation.Y, self.Translation.Z);
			Mat4f.Scale(other, other, self.ScaleXYZ.X, self.ScaleXYZ.Y, self.ScaleXYZ.Z);
			Mat4f.RotateX(other, other, GameMath.DEG2RAD * self.Rotation.X);
			Mat4f.RotateY(other, other, GameMath.DEG2RAD * self.Rotation.Y);
			Mat4f.RotateZ(other, other, GameMath.DEG2RAD * self.Rotation.Z);
			Mat4f.Translate(other, other, -self.Origin.X, -self.Origin.Y, -self.Origin.Z);
		}

		public static NatFloat[] RotateHorizontal(BlockFacing face, NatFloat[] northVector)
		{
			if(face == BlockFacing.NORTH) return northVector;
			var vector = Array.ConvertAll(northVector, f => f.Clone());
			switch(face.Code)
			{
				case "east":
					{
						var tmp = vector[0];
						vector[0] = vector[2];
						vector[2] = tmp;
						Invert(vector[0]);
					}
					break;
				case "west":
					{
						var tmp = vector[0];
						vector[0] = vector[2];
						vector[2] = tmp;
						Invert(vector[2]);
					}
					break;
				case "south":
					{
						Invert(vector[0]);
						Invert(vector[2]);
					}
					break;
			}
			return vector;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Invert(NatFloat value)
		{
			value.offset = -value.offset;
			value.avg = -value.avg;
		}
	}
}