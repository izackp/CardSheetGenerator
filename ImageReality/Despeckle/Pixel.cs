using System;

namespace ImageReality
{
	public struct Pixel
	{
		public ushort R;
		public ushort G;
		public ushort B;
		public ushort A;

		const double RGB_LUMINANCE_RED   = 0.2126;
		const double RGB_LUMINANCE_GREEN = 0.7152;
		const double RGB_LUMINANCE_BLUE  = 0.0722;

		public Pixel (ushort r, ushort g, ushort b, ushort a) {
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public double RGBLuminance() {
			return (R * RGB_LUMINANCE_RED + G * RGB_LUMINANCE_GREEN + B * RGB_LUMINANCE_BLUE);
		}
	}
}

