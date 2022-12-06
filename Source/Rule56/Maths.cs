using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace CombatAI
{
	public static class Maths
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float a, float b)
		{
			return a > b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Max(int a, int b)
		{
			return a > b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Max(short a, short b)
		{
			return a > b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Max(byte a, byte b)
		{
			return a > b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float a, float b, float c)
		{
			float temp;
			return (temp = a > b ? a : b) > c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Max(int a, int b, int c)
		{
			int temp;
			return (temp = a > b ? a : b) > c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Max(short a, short b, short c)
		{
			short temp;
			return (temp = a > b ? a : b) > c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Max(byte a, byte b, byte c)
		{
			byte temp;
			return (temp = a > b ? a : b) > c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float a, float b)
		{
			return a < b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Min(int a, int b)
		{
			return a < b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Min(short a, short b)
		{
			return a < b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Min(byte a, byte b)
		{
			return a < b ? a : b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float a, float b, float c)
		{
			float temp;
			return (temp = a < b ? a : b) < c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Min(int a, int b, int c)
		{
			int temp;
			return (temp = a < b ? a : b) < c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Min(short a, short b, short c)
		{
			short temp;
			return (temp = a < b ? a : b) < c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Min(byte a, byte b, byte c)
		{
			byte temp;
			return (temp = a < b ? a : b) < c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sqr(float a)
		{
			return a * a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Sqr(int a)
		{
			return a * a;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Sqr(short a)
		{
			return (short)(a * a);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Sqr(byte a)
		{
			return (byte)(a * a);
		}

		public static float Sqrt_Fast(float x, int iterations)
		{
			int n;
			int k;
			var a = (int)(x * 1024);
			if ((a & 0xFFFF0000) != 0)
			{
				if ((a & 0xFFF00000) != 0)
					n = 20;
				else
					n = 16;
			}
			else
			{
				if ((a & 0xFFFFF000) != 0)
					n = 12;
				else if ((a & 0xFFFFFFC0) != 0)
					n = 6;
				else
					n = 0;
			}

			k = a >> n;
			while (k != 0)
			{
				k = k >> 1;
				n++;
			}

			var bot = 1 << ((n - 1) >> 1);
			var top = bot << 1;
			while (iterations-- > 0)
			{
				var mid = (bot + top) >> 1;
				var midSqr = mid * mid;
				if (midSqr == a)
					return mid / 32f;
				else if (midSqr < a)
					bot = mid;
				else
					top = mid;
			}

			return (bot + top) / 64f;
		}

		public static int Sqrt_Fast(int a, int iterations)
		{
			int n;
			int k;
			if ((a & 0xFFFFFF00) != 0)
			{
				if ((a & 0xFFFFF000) != 0)
					n = 12;
				else
					n = 8;
			}
			else
			{
				if ((a & 0xFFFFFFF0) != 0)
					n = 4;
				else
					n = 0;
			}

			k = a >> n;
			while (k != 0)
			{
				k = k >> 1;
				n++;
			}

			var bot = 1 << ((n - 1) >> 1);
			var top = bot << 1;
			while (iterations-- > 0)
			{
				var mid = (bot + top) >> 1;
				var midSqr = mid * mid;
				if (midSqr == a)
					return mid;
				else if (midSqr < a)
					bot = mid;
				else
					top = mid;
			}

			return (bot + top) >> 1;
		}
	}
}