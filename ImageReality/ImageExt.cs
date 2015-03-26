using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ImageReality
{
	public class DrawableLine {

		public PointF Start;
		public PointF End;

		public DrawableLine(int x, int y, int x2, int y2) {
			Start.X = x;
			Start.Y = y;
			End.X = x2;
			End.Y = y2;
		}
	}
	public static class ImageExt
	{
		public static Image Resize(this Image source, int width, int height, ResamplingFilters upscaleFilter, ResamplingFilters downscaleFilter) {
			float scale = AspectFit (source.Width, source.Height, width, height);
			ResamplingFilters filterToUse = ResamplingFilters.Lanczos3;

			if (scale == 0)
				return source;
			
			if (scale > 0)
				filterToUse = upscaleFilter;
			else
				filterToUse = downscaleFilter;

			Rectangle drawRect = new Rectangle (0, 0, (int)(source.Width * scale), (int)(source.Height * scale));
			Bitmap bitmapSouce = new Bitmap(source);

			ResamplingService resamplingService = new ResamplingService();
			resamplingService.Filter = filterToUse;

			ushort[][,] input = ConvertBitmapToArray((Bitmap)bitmapSouce);
			ushort[][,] output = resamplingService.Resample(input, drawRect.Width, 
				drawRect.Height);

			Image imgResult = (Image)ConvertArrayToBitmap(output);

			return imgResult;
		}


		#region Private Methods

		/// <summary>
		/// Converts Bitmap to array. Supports only Format32bppArgb pixel format.
		/// </summary>
		/// <param name="bmp">Bitmap to convert.</param>
		/// <returns>Output array.</returns>
		private static ushort[][,] ConvertBitmapToArray(Bitmap bmp) {

			ushort[][,] array = new ushort[4][,];

			for (int i = 0; i < 4; i++)
				array[i] = new ushort[bmp.Width, bmp.Height];

			BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			int nOffset = (bd.Stride - bd.Width * 4);

			unsafe {

				byte* p = (byte*)bd.Scan0;

				for (int y = 0; y < bd.Height; y++) {
					for (int x = 0; x < bd.Width; x++) {

						array[3][x, y] = (ushort)p[3];
						array[2][x, y] = (ushort)p[2];
						array[1][x, y] = (ushort)p[1];
						array[0][x, y] = (ushort)p[0];

						p += 4;
					}

					p += nOffset;
				}
			}

			bmp.UnlockBits(bd);

			return array;
		}

		/// <summary>
		/// Converts array to Bitmap. Supports only Format32bppArgb pixel format.
		/// </summary>
		/// <param name="array">Array to convert.</param>
		/// <returns>Output Bitmap.</returns>
		private static Bitmap ConvertArrayToBitmap(ushort[][,] array) {

			int width = array[0].GetLength(0);
			int height = array[0].GetLength(1);

			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

			BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int nOffset = (bd.Stride - bd.Width * 4);

			unsafe {

				byte* p = (byte*)bd.Scan0;

				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {

						p[3] = (byte)Math.Min(Math.Max(array[3][x, y], Byte.MinValue), Byte.MaxValue);
						p[2] = (byte)Math.Min(Math.Max(array[2][x, y], Byte.MinValue), Byte.MaxValue);
						p[1] = (byte)Math.Min(Math.Max(array[1][x, y], Byte.MinValue), Byte.MaxValue);
						p[0] = (byte)Math.Min(Math.Max(array[0][x, y], Byte.MinValue), Byte.MaxValue);

						p += 4;
					}

					p += nOffset;
				}
			}

			bmp.UnlockBits(bd);

			return bmp;
		}

		#endregion

		public static float AspectFit(float sourceWidth, float sourceHeight, float destWidth, float destHeight) {
			float widthDiff = Math.Abs (destWidth - sourceWidth);
			float heightDiff = Math.Abs (destHeight - sourceHeight);

			float result = 0.0f;

			if (widthDiff < heightDiff) {
				result = destWidth / sourceWidth;
			} else {
				result = destHeight / sourceHeight;
			}

			return result;
		}

		public static Image DrawLines(this Image source, DrawableLine[] lines) {
			Bitmap newImage = new Bitmap(source);
			Pen pen = new Pen (Color.Black, 1.0f);
			using (Graphics gr = Graphics.FromImage(newImage))
			{
				foreach (DrawableLine line in lines) {
					gr.DrawLine (pen, line.Start, line.End);
				}
			}
			return newImage;
		}

		public static Image Trim(this Image source) {
			Bitmap bitmapSouce = new Bitmap (source);
			Rectangle srcRect = default(Rectangle);
			BitmapData data = null;
			try
			{
				data = bitmapSouce.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				byte[] buffer = new byte[data.Height * data.Stride];
				Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
				int xMin = int.MaxValue;
				int xMax = 0;
				int yMin = int.MaxValue;
				int yMax = 0;
				for (int y = 0; y < data.Height; y++)
				{
					for (int x = 0; x < data.Width; x++)
					{
						byte alpha = buffer[y * data.Stride + 4 * x + 3];
						if (alpha != 0)
						{
							if (x < xMin) xMin = x;
							if (x > xMax) xMax = x;
							if (y < yMin) yMin = y;
							if (y > yMax) yMax = y;
						}
					}
				}
				if (xMax < xMin || yMax < yMin)
				{
					// Image is empty...
					return null;
				}
				srcRect = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
			}
			finally
			{
				if (data != null)
					bitmapSouce.UnlockBits(data);
			}

			Bitmap dest = new Bitmap(srcRect.Width, srcRect.Height);
			Rectangle destRect = new Rectangle(0, 0, srcRect.Width, srcRect.Height);
			using (Graphics graphics = Graphics.FromImage(dest))
			{
				graphics.DrawImage(source, destRect, srcRect, GraphicsUnit.Pixel);
			}
			return dest;
		}

		public static Image Extent(this Image source, int width, int height, Color color) {

			Bitmap dest = new Bitmap(width, height);
			int extraWidth = width - source.Width;
			int extraHeight = height - source.Height;
			Rectangle destRect = new Rectangle((int)(extraWidth * 0.5), (int)(extraHeight * 0.5), source.Width, source.Height);
			SolidBrush coloredBrush = new SolidBrush(color);
			using (Graphics graphics = Graphics.FromImage(dest))
			{
				graphics.FillRectangle (coloredBrush, dest.Bounds ());
				graphics.DrawImage(source, destRect, source.Bounds(), GraphicsUnit.Pixel);
			}

			return dest;
		}

		public static Rectangle Bounds(this Image source) {
			return new Rectangle (0, 0, source.Width, source.Height);
		}

		//assumes all images are the same size
		public static Image TileImages(List<Image> images, int columns, int canvasWidth, int canvasHeight, int separatorSpace, int contentOffsetX, int contentOffsetY) {
			if (images.Count == 0)
				return null;

			double temp = (double)images.Count / columns;
			int rows = (int)temp;
			if (temp - rows > 0)
				rows += 1;

			int imageWidth = images [0].Width;
			int imageHeight = images [0].Height;
			int width = columns * imageWidth + (columns - 1) * separatorSpace;
			int height = rows * imageHeight + (rows - 1) * separatorSpace;

			//To Center the tiles
			int xOffset = (canvasWidth - width) / 2 + contentOffsetX;
			int yOffset = (canvasHeight - height) / 2 + contentOffsetY;

			Rectangle[] destRects = new Rectangle[rows * columns];
			for (int row = 0; row < rows; row += 1) {
				int separatorY = row * separatorSpace;
				for (int column = 0; column < columns; column += 1) {
					int separatorX = column * separatorSpace;
					destRects[row * columns + column] = new Rectangle(column * imageWidth + xOffset + separatorX, row * imageHeight + yOffset + separatorY, imageWidth, imageHeight);
				}
			}

			Bitmap tiledImageSheet = new Bitmap(canvasWidth, canvasHeight);
			SolidBrush coloredBrush = new SolidBrush(Color.White);
			using (Graphics graphics = Graphics.FromImage(tiledImageSheet))
			{
				graphics.FillRectangle (coloredBrush, tiledImageSheet.Bounds());
				for (int i = 0; i < images.Count; i += 1) {
					Image source = images [i];
					Rectangle dest = destRects [i];
					graphics.DrawImage (source, dest, source.Bounds (), GraphicsUnit.Pixel);
				}
			}
			return tiledImageSheet;
		}
	}
}

