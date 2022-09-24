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
                var value = (ulong)(values[i] + (bits[i] < 0 ? 1 << (bit - 1) : 0));
                data = (data <<= bit) | (value & (ulong)((1 << bit) - 1));
            }
            return (long)data;
        }
        public static long BuildLong(int[] bits, params long[] values)
        {
            ulong data = 0;
            for (var i = 0; i < bits.Length; i++)
            {
                var bit = Math.Abs(bits[i]);
                var value = (ulong)(values[i] + (bits[i] < 0 ? 1 << (bit - 1) : 0));
                data = (data <<= bit) | (value & (ulong)((1 << bit) - 1));
            }
            return (long)data;
        }

        public static int[] Expand(int[] bits, long packedData)
        {
            var array = new int[bits.Length];
            for (var i = bits.Length - 1; i >= 0; i--)
            {
                var bit = Math.Abs(bits[i]);
                array[i] = (int)((uint)(packedData & ((1 << bit) - 1)) - (bits[i] < 0 ? 1 << (bit - 1) : 0));
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
                array[i] = (long)((ulong)(packedData & ((1 << bit) - 1)) - (ulong)(bits[i] < 0 ? 1 << (bit - 1) : 0));
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

            public Vec3ToLong(float unit = 0.001f, Vector3? center = null, bool loop = false, byte x = 22, byte y = 20)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector3.zero;
                Bits = new int[] { -x, -y, -(64 - x - y) };
                Loop = loop;
            }
            public long Build(Vector3 vector)
            {
                vector -= Center;
                if (Loop)
                {
                    return BitPacking.BuildLong(Bits,
                        (long)(vector.x * ReciprocalUnit),
                        (long)(vector.y * ReciprocalUnit),
                        (long)(vector.z * ReciprocalUnit));
                }

                return BitPacking.BuildLong(Bits,
                    Clamp((long)(vector.x * ReciprocalUnit), Bits[0]),
                    Clamp((long)(vector.x * ReciprocalUnit), Bits[1]),
                    Clamp((long)(vector.x * ReciprocalUnit), Bits[2]));
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

            public Vec3ToInt(float unit = 0.01f, Vector3? center = null, bool loop = false, byte x = 11, byte y = 10)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector3.zero;
                Bits = new int[] { -x, -y, -(32 - x - y) };
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
                    (int)Clamp((int)(vector.x * ReciprocalUnit), Bits[1]),
                    (int)Clamp((int)(vector.x * ReciprocalUnit), Bits[2]));
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

            public Vec2ToInt(float unit = 0.001f, Vector2? center = null, bool loop = false, byte x = 16)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector2.zero;
                Bits = new int[] { -x, -(32 - x) };
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
                    (int)Clamp((int)(vector.x * ReciprocalUnit), Bits[1]));
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

            public Vec2ToShort(float unit = 0.001f, Vector2? center = null, bool loop = false, byte x = 8)
            {
                Unit = unit;
                ReciprocalUnit = Mathf.RoundToInt(1f / unit);
                Center = center.HasValue ? center.Value : Vector2.zero;
                Bits = new int[] { -x, -(16 - x) };
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
                    (short)Clamp((short)(vector.x * ReciprocalUnit), Bits[1]));
            }
            public Vector2 Expand(short value)
            {
                var array = BitPacking.Expand(Bits, value);
                return new Vector2(array[0] * Unit, array[1] * Unit);
            }
        }
        #endregion

        #region Utility
        public static long MaxValue(int bit) => bit > 0 ? (1 << bit) : (1 << bit - 1) - 1;
        public static long MinValue(int bit) => bit > 0 ? 0 : -(1 << bit - 1);
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
        private static readonly int[] BitsByInt = { 10, 10, 10 };
        const float UnitByInt = 0.5f;
        const float ReciprocalUnitByInt = 2f;

        private static readonly int[] BitsByLong = { 20, 20, 20 };
        const float UnitByLong = 0.0005f;
        const float ReciprocalUnitByLong = 2000f;

        public static int PackingToInt(this Quaternion quaternion) => EulerAnglesPackingToInt(quaternion.eulerAngles);
        public static int EulerAnglesPackingToInt(this Vector3 eulerAngles)
        {
            return BitPacking.BuildInt(BitsByInt,
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.x, 360f) * ReciprocalUnitByInt),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.y, 360f) * ReciprocalUnitByInt),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.z, 360f) * ReciprocalUnitByInt));
        }
        public static Quaternion ExpandToQuaternion(this int v)
        {
            var array = BitPacking.Expand(BitsByInt, v);
            return Quaternion.Euler(array[0] * UnitByInt, array[1] * UnitByInt, array[2] * UnitByInt);
        }

        public static long PackingToLong(this Quaternion quaternion) => EulerAnglesPackingToLong(quaternion.eulerAngles);
        public static long EulerAnglesPackingToLong(this Vector3 eulerAngles)
        {
            return BitPacking.BuildLong(BitsByLong,
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.x, 360f) * ReciprocalUnitByLong),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.y, 360f) * ReciprocalUnitByLong),
                Mathf.RoundToInt(Mathf.Repeat(eulerAngles.z, 360f) * ReciprocalUnitByLong));
        }
        public static Quaternion ExpandToQuaternion(this long v)
        {
            var array = BitPacking.Expand(BitsByLong, v);
            return Quaternion.Euler(array[0] * UnitByLong, array[1] * UnitByLong, array[2] * UnitByLong);
        }
    }
    public static class BitPackingColor
    {
        public static int PackingToInt(this Color color) => PackingToInt((Color32)color);
        public static int PackingToInt(this Color32 color) => color.r << 24 | color.g << 16 | color.b << 8 | color.a;
        public static Color32 ExpandToColor(this int v)
            => new Color32((byte)(v >> 24), (byte)(v >> 16 & 0xF), (byte)(v >> 8 & 0xF), (byte)(v & 0xF));
    }
}
