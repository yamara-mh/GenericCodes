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
                    return BitPacking.BuildLong(Bits,
                        (long)(vector.x * ReciprocalUnit),
                        (long)(vector.y * ReciprocalUnit),
                        (long)(vector.z * ReciprocalUnit));
                }

                return BitPacking.BuildLong(Bits,
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
    public static class BitPackingAnglesDirectionToInt
    {
        private static readonly int[] AnglesBits = { -15, -17 };
        const float AnglesUnitX = 0.0000625f;
        const float AnglesUnitY = 0.01f;
        const float ReciprocalAnglesUnitX = 16000f;
        const float ReciprocalAnglesUnitY = 100f;

        public static int AnglesDirectionPackingToInt(this Vector3 direction)
        {
            var dir = direction.normalized;
            var xz = new Vector2(-dir.x, -dir.z);
            return BitPacking.BuildInt(AnglesBits,
                Mathf.RoundToInt(dir.y * ReciprocalAnglesUnitX),
                Mathf.RoundToInt(Vector2.SignedAngle(xz, Vector2.down) * ReciprocalAnglesUnitY));
        }
        public static Vector3 ExpandToAnglesDirection(this int v)
        {
            var array = BitPacking.Expand(AnglesBits, v);
            var x = array[0] * AnglesUnitX;
            var y = array[1] * AnglesUnitY * Mathf.Deg2Rad;
            var yRate = Mathf.Sqrt(1f - x * x);
            return new Vector3(Mathf.Sin(y) * yRate, x, Mathf.Cos(y) * yRate);
        }


        private static readonly int[] UvBits = { 1, -15, -15 };
        const float UvUnit = 0.0000625f;
        const float UvReciprocalUnit = 16000;

        public static int UvDirectionPackingToInt(this Vector3 direction)
        {
            var dir = direction.normalized;
            return BitPacking.BuildInt(UvBits,
                dir.y >= 0f ? 1 : 0,
                Mathf.RoundToInt(dir.x * UvReciprocalUnit),
                Mathf.RoundToInt(dir.z * UvReciprocalUnit));
        }
        public static Vector3 ExpandToUvDirection(this int v)
        {
            var array = BitPacking.Expand(UvBits, v);
            var xz = new Vector2(array[1] * UvUnit, array[2] * UvUnit);
            var magnitude = xz.magnitude;
            return new Vector3(xz.x, Mathf.Sqrt(1f - magnitude * magnitude) * (array[0] == 1 ? 1f : -1f), xz.y);
        }


        private static readonly int[] Bits = { 1, -14, -17 };
        private static readonly int[] Bits2 = { 1, 1, -15, -15 };
        private const float Cos30 = 0.8660254f;
        private const float ReciprocalCos30 = 1.1547005f;

        public static int DirectionPackingToInt(this Vector3 direction)
        {
            var dir = direction.normalized;
            if (Mathf.Abs(dir.y) < 0.5f)
            {
                var xz = new Vector2(-dir.x, -dir.z);
                return BitPacking.BuildInt(Bits, 0,
                    Mathf.RoundToInt(dir.y * ReciprocalAnglesUnitX),
                    Mathf.RoundToInt(Vector2.SignedAngle(xz, Vector2.down) * ReciprocalAnglesUnitY));
            }
            dir.x *= ReciprocalCos30;
            dir.z *= ReciprocalCos30;
            return BitPacking.BuildInt(Bits2, 1,
                dir.y >= 0f ? 1 : 0,
                Mathf.RoundToInt(dir.x * UvReciprocalUnit),
                Mathf.RoundToInt(dir.z * UvReciprocalUnit));
        }
        public static Vector3 ExpandToDirection(this int v)
        {
            if (v >> 31 == 0)
            {
                var array = BitPacking.Expand(Bits, v);
                var x = array[1] * AnglesUnitX;
                var y = array[2] * AnglesUnitY * Mathf.Deg2Rad;
                var yRate = Mathf.Sqrt(1f - x * x);
                return new Vector3(Mathf.Sin(y) * yRate, x, Mathf.Cos(y) * yRate);
            }
            var uvArray = BitPacking.Expand(Bits2, v);
            var xz = new Vector2(uvArray[2] * UvUnit, uvArray[3] * UvUnit) * Cos30;
            var magnitude = xz.magnitude;
            return new Vector3(xz.x, Mathf.Sqrt(1f - magnitude * magnitude) * (uvArray[1] == 1 ? 1f : -1f), xz.y);
        }
    }

    public static class BitPackingDirectionToLong
    {
        private static readonly int[] AnglesBits = { -32, -32 };
        const float AnglesUnit = 5e-10f;
        const float ReciprocalAnglesUnit = 2000000000f;

        public static long AnglesDirectionPackingToLong(this Vector3 direction)
        {
            var dir = direction.normalized;
            var xz = new Vector2(-dir.x, -dir.z);
            return BitPacking.BuildLong(AnglesBits,
                Mathf.RoundToInt(dir.y * ReciprocalAnglesUnit),
                Mathf.RoundToInt(Vector2.SignedAngle(xz, Vector2.down) * ReciprocalAnglesUnit));
        }
        public static Vector3 ExpandToAnglesDirection(this long v)
        {
            var array = BitPacking.Expand(AnglesBits, v);
            var x = array[0] * AnglesUnit;
            var y = array[1] * AnglesUnit * Mathf.Deg2Rad;
            var yRate = Mathf.Sqrt(1f - x * x);
            return new Vector3(Mathf.Sin(y) * yRate, x, Mathf.Cos(y) * yRate);
        }


        private static readonly int[] UvBits = { 1, -31, -31 };
        const float UvUnit = 1e-9f;
        const float UvReciprocalUnit = 1000000000f;

        public static long UvDirectionPackingToLong(this Vector3 direction)
        {
            var dir = direction.normalized;
            return BitPacking.BuildLong(UvBits,
                dir.y >= 0f ? 1 : 0,
                Mathf.RoundToInt(dir.x * UvReciprocalUnit),
                Mathf.RoundToInt(dir.z * UvReciprocalUnit));
        }
        public static Vector3 ExpandToUvDirection(this long v)
        {
            var array = BitPacking.Expand(UvBits, v);
            var xz = new Vector2(array[1] * UvUnit, array[2] * UvUnit);
            var magnitude = xz.magnitude;
            return new Vector3(xz.x, Mathf.Sqrt(1f - magnitude * magnitude) * (array[0] == 1 ? 1f : -1f), xz.y);
        }


        private static readonly int[] Bits = { 1, 1, -31, -31 };
        private const float Cos30 = 0.8660254f;
        private const float ReciprocalCos30 = 1.1547005f;

        public static long DirectionPackingToLong(this Vector3 direction)
        {
            var dir = direction.normalized;
            if (Mathf.Abs(dir.y) < 0.5f)
            {
                var xz = new Vector2(-dir.x, -dir.z);
                return BitPacking.BuildLong(Bits, 0, 0,
                    Mathf.RoundToInt(dir.y * ReciprocalAnglesUnit),
                    Mathf.RoundToInt(Vector2.SignedAngle(xz, Vector2.down) * ReciprocalAnglesUnit));
            }
            dir.x *= ReciprocalCos30;
            dir.z *= ReciprocalCos30;
            return BitPacking.BuildLong(Bits, 1,
                dir.y >= 0f ? 1 : 0,
                Mathf.RoundToInt(dir.x * UvReciprocalUnit),
                Mathf.RoundToInt(dir.z * UvReciprocalUnit));
        }
        public static Vector3 ExpandToDirection(this long v)
        {
            if (v >> 63 == 0)
            {
                var array = BitPacking.Expand(Bits, v);
                var x = array[2] * AnglesUnit;
                var y = array[3] * AnglesUnit * Mathf.Deg2Rad;
                var yRate = Mathf.Sqrt(1f - x * x);
                return new Vector3(Mathf.Sin(y) * yRate, x, Mathf.Cos(y) * yRate);
            }
            var uvArray = BitPacking.Expand(Bits, v);
            var xz = new Vector2(uvArray[2] * UvUnit, uvArray[3] * UvUnit) * Cos30;
            var magnitude = xz.magnitude;
            return new Vector3(xz.x, Mathf.Sqrt(1f - magnitude * magnitude) * (uvArray[1] == 1 ? 1f : -1f), xz.y);
        }
    }
    public static class BitPakingQuaternion
    {
        private static readonly int[] BitsByInt = { 10, 10, 10 };
        const float UnitByInt = 0.5f;
        const float ReciprocalUnitByInt = 2f;

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


        private static readonly int[] BitsByLong = { 20, 20, 20 };
        const float UnitByLong = 0.0005f;
        const float ReciprocalUnitByLong = 2000f;

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
