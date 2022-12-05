using System;
using System.Runtime.CompilerServices;

namespace CombatAI
{
	public static class Maths
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float a, float b)	=> a > b ? a : b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Max(int a, int b)			=> a > b ? a : b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Max(short a, short b)	=> a > b ? a : b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Max(byte a, byte b)		=> a > b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float a, float b, float c)
		{
			float temp;
			return (temp = (a > b ? a : b)) > c ? temp : c;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Max(int a, int b, int c)
		{
			int temp;
			return (temp = (a > b ? a : b)) > c ? temp : c;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Max(short a, short b, short c)
		{
			short temp;
			return (temp = (a > b ? a : b)) > c ? temp : c;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Max(byte a, byte b, byte c)
		{
			byte temp;
			return (temp = (a > b ? a : b)) > c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float a, float b)	=> a < b ? a : b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Min(int a, int b)			=> a < b ? a : b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Min(short a, short b)	=> a < b ? a : b;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Min(byte a, byte b)		=> a < b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float a, float b, float c)
		{
			float temp;
			return (temp = (a < b ? a : b)) < c ? temp : c;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Min(int a, int b, int c)
		{
			int temp;
			return (temp = (a < b ? a : b)) < c ? temp : c;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Min(short a, short b, short c)
		{
			short temp;
			return (temp = (a < b ? a : b)) < c ? temp : c;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Min(byte a, byte b, byte c)
		{
			byte temp;
			return (temp = (a < b ? a : b)) < c ? temp : c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sqr(float a)	=> a * a;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Sqr(int a)		=> a * a;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Sqr(short a)	=> (short)(a * a);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Sqr(byte a)		=> (byte) (a * a);

	}
}
