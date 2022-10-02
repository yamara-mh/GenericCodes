using System;
using UnityEngine;

namespace Network
{
    public static class BitPacking
    {
        #region Base
        public static byte BuildByte(int[] bits, params int[] values) => (byte)BuildLong(bits, values);
        public static short BuildShort(int[] bits, params int[] values) => (short)BuildLong(bits, values);
        public static int BuildInt(int[] bits, params int[] values) => (int)BuildLong(bits, values);
        public static long BuildLong(int[] bits, params int[] values)
        {
            ulong data = 0;
            for (var i = 0; i < bits.Length; i++)
            {
                var bit = Math.Abs(bits[i]);
                var value = (ulong)(values[i] + (bits[i] < 0 && bit < 32 ? MaxValue(bits[i]) : 0));
                data = (data <<= bit) | (value & (uint)(((ulong)1 << bit) - 1));
            }
            return (long)data;
        }
        public static long BuildLong(int[] bits, params long[] values)
        {
            ulong data = 0;
            for (var i = 0; i < bits.Length; i++)
            {
                var bit = Math.Abs(bits[i]);
                var value = (ulong)(values[i] + (bits[i] < 0 && bit < 32 ? MaxValue(bits[i]) : 0));
                data = (data <<= bit) | (value & (((ulong)1 << bit) - 1));
            }
            return (long)data;
        }

        public static int[] Expand(int[] bits, long packedData)
        {
            var array = new int[bits.Length];
            for (var i = bits.Length - 1; i >= 0; i--)
            {
                var bit = Math.Abs(bits[i]);
                array[i] = (int)((uint)(packedData & (((long)1 << bit) - 1)) - (bits[i] < 0 && bit < 32 ? MaxValue(bits[i]) : 0));
                packedData >>= bit;
            }
            return array;
        }
        public static long[] ExpandToLongs(int[] bits, long packedData)
        {
            var array = new long[bits.Length];
            for (var i = bits.Length - 1; i >= 0; i--)
            {
                var bit = Math.Abs(bits[i]);
                array[i] = (long)(ulong)((packedData & (((long)1 << bit) - 1)) - (bits[i] < 0 && bit < 64  ? MaxValue(bits[i]) : 0));
                packedData >>= bit;
            }
            return array;
        }
        #endregion

        #region Vector
        public class Vec3ToLong
        {
            private readonly float Unit;
            private readonly int ReciprocalUnit;
            private readonly Vector3 Center;
            private readonly int[] Bits;
            private readonly bool Loop;

            public Vec3ToLong(float unit = 0.001f, Vector3? center = null, bool loop = false, int x = -22, int y = -20, int z = -22)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector3.zero;
                Bits = new int[] { x, y, z };
                Loop = loop;
            }
            public long Build(Vector3 vector)
            {
                vector -= Center;
                if (Loop)
                {
                    return BuildLong(Bits,
                        (long)(vector.x * ReciprocalUnit),
                        (long)(vector.y * ReciprocalUnit),
                        (long)(vector.z * ReciprocalUnit));
                }

                return BuildLong(Bits,
                    Clamp((long)(vector.x * ReciprocalUnit), Bits[0]),
                    Clamp((long)(vector.y * ReciprocalUnit), Bits[1]),
                    Clamp((long)(vector.z * ReciprocalUnit), Bits[2]));
            }
            public Vector3 Expand(long value)
            {
                var array = BitPacking.Expand(Bits, value);
                return new Vector3(array[0] * Unit, array[1] * Unit, array[2] * Unit);
            }
        }
        public class Vec3ToInt
        {
            private readonly float Unit;
            private readonly int ReciprocalUnit;
            private readonly Vector3 Center;
            private readonly int[] Bits;
            private readonly bool Loop;

            public Vec3ToInt(float unit = 0.01f, Vector3? center = null, bool loop = false, int x = -11, int y = -10, int z = -11)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector3.zero;
                Bits = new int[] { x, y, z };
                Loop = loop;
            }
            public int Build(Vector3 vector)
            {
                vector -= Center;
                if (Loop)
                {
                    return BuildInt(Bits,
                        (int)(vector.x * ReciprocalUnit),
                        (int)(vector.y * ReciprocalUnit),
                        (int)(vector.z * ReciprocalUnit));
                }

                return BuildInt(Bits,
                    (int)Clamp((int)(vector.x * ReciprocalUnit), Bits[0]),
                    (int)Clamp((int)(vector.y * ReciprocalUnit), Bits[1]),
                    (int)Clamp((int)(vector.z * ReciprocalUnit), Bits[2]));
            }
            public Vector3 Expand(int value)
            {
                var array = BitPacking.Expand(Bits, value);
                return new Vector3(array[0] * Unit, array[1] * Unit, array[2] * Unit);
            }
        }
        public class Vec2ToInt
        {
            private readonly float Unit;
            private readonly int ReciprocalUnit;
            private readonly Vector2 Center;
            private readonly int[] Bits;
            private readonly bool Loop;

            public Vec2ToInt(float unit = 0.001f, Vector2? center = null, bool loop = false, int x = -16, int y = -16)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector2.zero;
                Bits = new int[] { x, y };
                Loop = loop;
            }
            public int Build(Vector2 vector)
            {
                vector -= Center;
                if (Loop)
                {
                    return BuildInt(Bits,
                        (int)(vector.x * ReciprocalUnit),
                        (int)(vector.y * ReciprocalUnit));
                }

                return BuildInt(Bits,
                    (int)Clamp((int)(vector.x * ReciprocalUnit), Bits[0]),
                    (int)Clamp((int)(vector.y * ReciprocalUnit), Bits[1]));
            }
            public Vector2 Expand(int value)
            {
                var array = BitPacking.Expand(Bits, value);
                return new Vector2(array[0] * Unit, array[1] * Unit);
            }
        }
        public class Vec2ToShort
        {
            private readonly float Unit;
            private readonly int ReciprocalUnit;
            private readonly Vector2 Center;
            private readonly int[] Bits;
            private readonly bool Loop;

            public Vec2ToShort(float unit = 0.001f, Vector2? center = null, bool loop = false, int x = -8, int y = -8)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector2.zero;
                Bits = new int[] { x, y };
                Loop = loop;
            }
            public int Build(Vector2 vector)
            {
                vector -= Center;
                if (Loop)
                {
                    return BuildShort(Bits,
                        (short)(vector.x * ReciprocalUnit),
                        (short)(vector.y * ReciprocalUnit));
                }

                return BuildInt(Bits,
                    (short)Clamp((short)(vector.x * ReciprocalUnit), Bits[0]),
                    (short)Clamp((short)(vector.y * ReciprocalUnit), Bits[1]));
            }
            public Vector2 Expand(short value)
            {
                var array = BitPacking.Expand(Bits, value);
                return new Vector2(array[0] * Unit, array[1] * Unit);
            }
        }
        #endregion

        #region Utility
        public static long MaxValue(int bit) => bit > 0 ? (1 << bit) : (1 << (-bit - 1)) - 1;
        public static long MinValue(int bit) => bit > 0 ? 0 : -(1 << (-bit - 1));
        public static long Clamp(long value, int bit) => Math.Max(MinValue(bit), Math.Min(MaxValue(bit), value));
        #endregion
    }
    public static class BitPackingAngle
    {
        const float Unit = 0.01f;
        const float ReciprocalUnit = 100f;

        public static short AnglePackingToShort(this float eulerAngle)
            => (short)(ushort)Mathf.RoundToInt(Mathf.Repeat(eulerAngle, 360f) * ReciprocalUnit);
        public static float ExpandToAngle(this short v) => (ushort)v * Unit;
        public static Quaternion ExpandToQuaternion(this short v, Vector3 axis) => Quaternion.Euler(axis * ExpandToAngle(v));
    }
    public static class BitPakingQuaternion
    {
        private static readonly int[] IntBits = { 10, 10, 10 };
        private static readonly int[] LongBIts = { 20, 20, 20 };
        private static readonly int[] DirIntBits = { 16, 16 };
        private static readonly int[] DirLongBits = { 32, 32 };

        public static int PackingToInt(this Quaternion quaternion) => EulerAnglesPackingToInt(quaternion.eulerAngles);
        public static int EulerAnglesPackingToInt(this Vector3 eulerAngles)
        {
            return BitPacking.BuildInt(IntBits,
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.x, 360f) * 2f),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.y, 360f) * 2f),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.z, 360f) * 2f));
        }
        public static Quaternion ExpandToQuaternion(this int v)
        {
            var array = BitPacking.Expand(IntBits, v);
            return Quaternion.Euler(array[0] * 0.5f, array[1] * 0.5f, array[2] * 0.5f);
        }

        public static long PackingToLong(this Quaternion quaternion) => EulerAnglesPackingToLong(quaternion.eulerAngles);
        public static long EulerAnglesPackingToLong(this Vector3 eulerAngles)
        {
            return BitPacking.BuildLong(LongBIts,
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.x, 360f) * 2000f),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.y, 360f) * 2000f),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.z, 360f) * 2000f));
        }
        public static Quaternion ExpandToQuaternion(this long v)
        {
            var array = BitPacking.Expand(LongBIts, v);
            return Quaternion.Euler(array[0] * 0.0005f, array[1] * 0.0005f, array[2] * 0.0005f);
        }

        public static int DirectionPackingToInt(this Vector3 direction)
        {
            var eulerAngles = Quaternion.LookRotation(direction).eulerAngles;
            return BitPacking.BuildInt(DirIntBits,
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.x, 360f) * 100f),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.y, 360f) * 100f));
        }
        public static Quaternion ExpandToDirectionQuaternion(this int v)
        {
            var array = BitPacking.Expand(DirIntBits, v);
            return Quaternion.Euler(array[0] * 0.01f, array[1] * 0.01f, 0f);
        }
        public static Vector3 ExpandToDirection(this int v) => ExpandToDirectionQuaternion(v) * Vector3.forward;

        public static long DirectionPackingToLong(this Vector3 direction)
        {
            var eulerAngles = Quaternion.LookRotation(direction).eulerAngles;
            return BitPacking.BuildLong(DirLongBits,
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.x, 360f) * 10000000f),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.y, 360f) * 10000000f));
        }
        public static Quaternion ExpandToDirectionQuaternion(this long v)
        {
            var array = BitPacking.Expand(DirLongBits, v);
            return Quaternion.Euler(array[0] * 0.0000001f, array[1] * 0.0000001f, 0f);
        }
        public static Vector3 ExpandToDirection(this long v) => ExpandToDirectionQuaternion(v) * Vector3.forward;
    }
    public static class BitPackingColor
    {
        public static int PackingToInt(this Color color) => PackingToInt((Color32)color);
        public static int PackingToInt(this Color32 color) => color.r << 24 | color.g << 16 | color.b << 8 | color.a;
        public static Color32 ExpandToColor(this int v)
            => new Color32((byte)(v >> 24), (byte)(v >> 16 & 0xF), (byte)(v >> 8 & 0xF), (byte)(v & 0xF));
    }
}
