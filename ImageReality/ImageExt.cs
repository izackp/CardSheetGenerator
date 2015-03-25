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
		public static Image Resize(this Image source, int width, int height) {
			Bitmap newImage = new Bitmap(width, height);

			float scale = AspectFit (source.Width, source.Height, width, height);

			Rectangle drawRect = new Rectangle (0, 0, (int)(source.Width * scale), (int)(source.Height * scale));
			//center
			drawRect.X = (int)((width - drawRect.Width) * 0.5);
			drawRect.Y = (int)((height - drawRect.Height) * 0.5);

			using (Graphics gr = Graphics.FromImage(newImage))
			{
				gr.SmoothingMode = SmoothingMode.HighQuality;
				gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
				gr.DrawImage(source, drawRect, new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
			}

			return newImage;
		}

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
		public static Image TileImages(List<Image> images, int columns) {
			if (images.Count == 0)
				return null;

			double temp = (double)images.Count / columns;
			int rows = (int)temp;
			if (temp - rows > 0)
				rows += 1;

			int imageWidth = images [0].Width;
			int imageHeight = images [0].Height;
			int width = columns * imageWidth;
			int height = rows * imageHeight;

			Rectangle[] destRects = new Rectangle[rows * columns];
			for (int row = 0; row < rows; row += 1) {
				for (int column = 0; column < columns; column += 1) {
					destRects[row * columns + column] = new Rectangle(column * imageWidth, row * imageHeight, imageWidth, imageHeight);
				}
			}

			Bitmap tiledImageSheet = new Bitmap(width, height);
			using (Graphics graphics = Graphics.FromImage(tiledImageSheet))
			{
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

