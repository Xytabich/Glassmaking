using System.IO;
using Vintagestory.API.Common;

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
    }
}