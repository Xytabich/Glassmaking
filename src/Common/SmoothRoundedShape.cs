using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking
{
    public class SmoothRoundedShape
    {
        [JsonProperty(Required = Required.Always)]
        public int segments;
        [JsonProperty, JsonConverter(typeof(ShapePartConverter))]
        public ShapePart[] inner = null;
        [JsonProperty(Required = Required.Always), JsonConverter(typeof(ShapePartConverter))]
        public ShapePart[] outer;

        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(segments);
            writer.Write(outer.Length);
            for(int i = 0; i < outer.Length; i++)
            {
                outer[i].ToBytes(writer);
            }
            if(inner == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(inner.Length);
                for(int i = 0; i < inner.Length; i++)
                {
                    inner[i].ToBytes(writer);
                }
            }
        }

        public void FromBytes(BinaryReader reader)
        {
            segments = reader.ReadInt32();
            outer = new ShapePart[reader.ReadInt32()];
            for(int i = 0; i < outer.Length; i++)
            {
                outer[i] = new ShapePart();
                outer[i].FromBytes(reader);
            }
            int innerCount = reader.ReadInt32();
            if(innerCount == 0) inner = null;
            else
            {
                inner = new ShapePart[innerCount];
                for(int i = 0; i < innerCount; i++)
                {
                    inner[i] = new ShapePart();
                    inner[i].FromBytes(reader);
                }
            }
        }

        public SmoothRoundedShape Clone()
        {
            return new SmoothRoundedShape() {
                segments = segments,
                outer = Array.ConvertAll(outer, x => x.Clone()),
                inner = inner == null ? null : Array.ConvertAll(inner, x => x.Clone())
            };
        }

        public static void BuildLerped(MeshData mesh, SmoothRoundedShape from, SmoothRoundedShape to, float t)
        {
            int segments = (int)Math.Ceiling(GameMath.Lerp(from.segments, to.segments, t));

            var tmpList = new FastList<FastVec2f>();

            float fromStep = (float)from.segments / segments;
            float toStep = (float)to.segments / segments;
            for(int i = 0; i < segments; i++)
            {
                AddLerpedVertex(mesh, tmpList, from, to, i * fromStep, i * toStep, t);
            }
            AddLerpedVertex(mesh, tmpList, from, to, from.segments, to.segments, t);
        }

        private static void AddLerpedVertex(MeshData mesh, FastList<FastVec2f> tmpList, SmoothRoundedShape from, SmoothRoundedShape to, float at, float bt, float t)
        {
            var a = LerpParts(from.outer, tmpList, from.segments, at);
            var b = LerpParts(to.outer, tmpList, to.segments, bt);
            var c = FastVec2f.Lerp(a, b, t);
            if(c.Y == 0f)
            {
                mesh.AddVertex(0, 0, c.X);
            }
            else
            {
                float step = GameMath.TWOPI / 8;
                for(int i = 0; i <= 8; i++)
                {
                    float angle = step * i;
                    mesh.AddVertex(GameMath.FastSin(angle) * c.Y, GameMath.FastCos(angle) * c.Y, c.X);
                }
            }
        }

        private static FastVec2f LerpParts(ShapePart[] parts, FastList<FastVec2f> tmpList, float full, float t)
        {
            tmpList.Clear();
            if(parts.Length == 1)
            {
                parts[0].Interpolate(tmpList, t / full);
                return tmpList[0];
            }
            else
            {
                for(int i = 0; i < parts.Length; i++)
                {
                    if(t <= parts[i].segments)
                    {
                        parts[0].Interpolate(tmpList, t / parts[i].segments);
                        return tmpList[0];
                    }
                    else
                    {
                        t -= parts[i].segments;
                    }
                }
                parts[parts.Length - 1].Interpolate(tmpList, 1);
                return tmpList[0];
            }
        }

        public class ShapePart
        {
            [JsonProperty(Required = Required.Always)]
            public int segments;
            [JsonProperty(Required = Required.Always)]
            public float[][] vertices;

            public void Interpolate(FastList<FastVec2f> tmpList, float t)
            {
                for(int i = 0; i < vertices.Length; i++)
                {
                    tmpList.Add(new FastVec2f(vertices[i]));
                }
                while(tmpList.Count > 1)
                {
                    int count = tmpList.Count - 1;
                    for(int i = 0; i < count; i++)
                    {
                        tmpList[i] = FastVec2f.Lerp(tmpList[i], tmpList[i + 1], t);
                    }
                    tmpList.RemoveAt(count);
                }
            }

            public void ToBytes(BinaryWriter writer)
            {
                writer.Write(segments);
                writer.Write(vertices.Length);
                for(int i = 0; i < vertices.Length; i++)
                {
                    writer.Write(vertices[i][0]);
                    writer.Write(vertices[i][1]);
                }
            }

            public void FromBytes(BinaryReader reader)
            {
                segments = reader.ReadInt32();
                vertices = new float[reader.ReadInt32()][];
                for(int i = 0; i < vertices.Length; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    vertices[i] = new float[2] { x, y };
                }
            }

            public ShapePart Clone()
            {
                return new ShapePart() {
                    segments = segments,
                    vertices = Array.ConvertAll(vertices, v => new float[] { v[0], v[1] })
                };
            }
        }

        private class ShapePartConverter : JsonConverter
        {
            public override bool CanWrite => false;

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ShapePart[]);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if(reader.TokenType == JsonToken.Null) return null;
                var arr = JArray.Load(reader);
                if(arr.Count > 0)
                {
                    if(arr[0].Type == JTokenType.Object)
                    {
                        return arr.ToObject<ShapePart[]>();
                    }
                    return new ShapePart[] { new ShapePart() { vertices = arr.ToObject<float[][]>() } };
                }
                return new ShapePart[0];
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}