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
    }
}