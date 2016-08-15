using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using ImageProcessor.Common.Exceptions;

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

		public static Bitmap Despeckle (this Image source, int adaptRadius, DespeckleFilterType filterType, int whiteLevel, int blackLevel) {
			Pixel[,] pixels = ConvertBitmapToPixelArray ((Bitmap)source);
			Pixel[,] despeckeled = DespeckleHistogram.DespeckleMedian (pixels, adaptRadius, filterType, whiteLevel, blackLevel);
			return ConvertPixelArrayToBitmap (despeckeled);
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

		private static Pixel[,] ConvertBitmapToPixelArray(Bitmap bmp) {

			ushort[][,] array = ConvertBitmapToArray (bmp);
			ushort[,] oneDimension = array [0];
			int xLength = oneDimension.GetLength (0);
			int yLength = oneDimension.GetLength (1);

			Pixel[,] pixels = new Pixel[xLength, yLength];

			for (int x = 0; x < xLength; x++) {
				for (int y = 0; y < yLength; y++) {
					ushort r = array [0] [x, y];
					ushort g = array [1] [x, y];
					ushort b = array [2] [x, y];
					ushort a = array [3] [x, y];
					pixels [x, y] = new Pixel (r, g, b, a);
				}
			}

			return pixels;
		}

		private static Bitmap ConvertPixelArrayToBitmap(Pixel[,] array) {
			
			int xLength = array.GetLength (0);
			int yLength = array.GetLength (1);

			ushort[][,] bitArray = new ushort[4][,];

			for (int i = 0; i < 4; i++)
				bitArray[i] = new ushort[xLength, yLength];

			Pixel[,] pixels = new Pixel[xLength, yLength];

			for (int x = 0; x < xLength; x++) {
				for (int y = 0; y < yLength; y++) {
					Pixel currPixel = pixels [x, y];
					bitArray [0] [x, y] = currPixel.R;
					bitArray [1] [x, y] = currPixel.G;
					bitArray [2] [x, y] = currPixel.B;
					bitArray [3] [x, y] = currPixel.A;
				}
			}

			return ConvertArrayToBitmap(bitArray);
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

		public static Bitmap RotateImage(this Image image, float angle)
		{
			if(image == null)
				throw new ArgumentNullException("image");

			const double pi2 = Math.PI / 2.0;


			double oldWidth = (double) image.Width;
			double oldHeight = (double) image.Height;

			// Convert degrees to radians
			double theta = ((double) angle) * Math.PI / 180.0;
			double locked_theta = theta;

			// Ensure theta is now [0, 2pi)
			while( locked_theta < 0.0 )
				locked_theta += 2 * Math.PI;

			double newWidth, newHeight; 
			int nWidth, nHeight; // The newWidth/newHeight expressed as ints



			double adjacentTop, oppositeTop;
			double adjacentBottom, oppositeBottom;


			if( (locked_theta >= 0.0 && locked_theta < pi2) ||
				(locked_theta >= Math.PI && locked_theta < (Math.PI + pi2) ) )
			{
				adjacentTop = Math.Abs(Math.Cos(locked_theta)) * oldWidth;
				oppositeTop = Math.Abs(Math.Sin(locked_theta)) * oldWidth;

				adjacentBottom = Math.Abs(Math.Cos(locked_theta)) * oldHeight;
				oppositeBottom = Math.Abs(Math.Sin(locked_theta)) * oldHeight;
			}
			else
			{
				adjacentTop = Math.Abs(Math.Sin(locked_theta)) * oldHeight;
				oppositeTop = Math.Abs(Math.Cos(locked_theta)) * oldHeight;

				adjacentBottom = Math.Abs(Math.Sin(locked_theta)) * oldWidth;
				oppositeBottom = Math.Abs(Math.Cos(locked_theta)) * oldWidth;
			}

			newWidth = adjacentTop + oppositeBottom;
			newHeight = adjacentBottom + oppositeTop;

			nWidth = (int) Math.Ceiling(newWidth);
			nHeight = (int) Math.Ceiling(newHeight);

			Bitmap rotatedBmp = new Bitmap(nWidth, nHeight);

			using(Graphics g = Graphics.FromImage(rotatedBmp))
			{

				Point [] points;

				if( locked_theta >= 0.0 && locked_theta < pi2 )
				{
					points = new Point[] { 
						new Point( (int) oppositeBottom, 0 ), 
						new Point( nWidth, (int) oppositeTop ),
						new Point( 0, (int) adjacentBottom )
					};

				}
				else if( locked_theta >= pi2 && locked_theta < Math.PI )
				{
					points = new Point[] { 
						new Point( nWidth, (int) oppositeTop ),
						new Point( (int) adjacentTop, nHeight ),
						new Point( (int) oppositeBottom, 0 )                        
					};
				}
				else if( locked_theta >= Math.PI && locked_theta < (Math.PI + pi2) )
				{
					points = new Point[] { 
						new Point( (int) adjacentTop, nHeight ), 
						new Point( 0, (int) adjacentBottom ),
						new Point( nWidth, (int) oppositeTop )
					};
				}
				else
				{
					points = new Point[] { 
						new Point( 0, (int) adjacentBottom ), 
						new Point( (int) oppositeBottom, 0 ),
						new Point( (int) adjacentTop, nHeight )        
					};
				}

				g.DrawImage(image, points);
			}

			return rotatedBmp;
		}

		public static Bitmap Alpha(this Image source, int percentage, Rectangle? rectangle = null)
		{
			return ImageProcessor.Imaging.Helpers.Adjustments.Alpha(source, percentage, rectangle);
		}

		public static Bitmap Brightness(Image source, int threshold, Rectangle? rectangle = null)
		{
			return ImageProcessor.Imaging.Helpers.Adjustments.Brightness(source, threshold, rectangle);
		}
			
		public static Bitmap Contrast(Image source, int threshold, Rectangle? rectangle = null)
		{
			return ImageProcessor.Imaging.Helpers.Adjustments.Contrast(source, threshold, rectangle);
		}
			
		public static Bitmap Gamma(Image source, float value)
		{
			return ImageProcessor.Imaging.Helpers.Adjustments.Gamma(source, value);
		}

		public static Bitmap Crop(this Image source, ImageProcessor.Imaging.CropLayer cropLayer) 
		{
			Bitmap newImage = null;
			Image image = source;
			try
			{
				int sourceWidth = image.Width;
				int sourceHeight = image.Height;
				RectangleF rectangleF;

				if (cropLayer.CropMode == ImageProcessor.Imaging.CropMode.Percentage)
				{
					// Fix for whole numbers. 
					cropLayer.Left = cropLayer.Left > 1 ? cropLayer.Left / 100 : cropLayer.Left;
					cropLayer.Right = cropLayer.Right > 1 ? cropLayer.Right / 100 : cropLayer.Right;
					cropLayer.Top = cropLayer.Top > 1 ? cropLayer.Top / 100 : cropLayer.Top;
					cropLayer.Bottom = cropLayer.Bottom > 1 ? cropLayer.Bottom / 100 : cropLayer.Bottom;

					// Work out the percentages.
					float left = cropLayer.Left * sourceWidth;
					float top = cropLayer.Top * sourceHeight;
					float width = cropLayer.Right < 1 ? (1 - cropLayer.Left - cropLayer.Right) * sourceWidth : sourceWidth;
					float height = cropLayer.Bottom < 1 ? (1 - cropLayer.Top - cropLayer.Bottom) * sourceHeight : sourceHeight;

					rectangleF = new RectangleF(left, top, width, height);
				}
				else
				{
					rectangleF = new RectangleF(cropLayer.Left, cropLayer.Top, cropLayer.Right, cropLayer.Bottom);
				}

				Rectangle rectangle = Rectangle.Round(rectangleF);

				if (rectangle.X < sourceWidth && rectangle.Y < sourceHeight)
				{
					if (rectangle.Width > (sourceWidth - rectangle.X))
					{
						rectangle.Width = sourceWidth - rectangle.X;
					}

					if (rectangle.Height > (sourceHeight - rectangle.Y))
					{
						rectangle.Height = sourceHeight - rectangle.Y;
					}

					newImage = new Bitmap(rectangle.Width, rectangle.Height);
					newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

					using (Graphics graphics = Graphics.FromImage(newImage))
					{
						graphics.SmoothingMode = SmoothingMode.AntiAlias;
						graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
						graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
						graphics.CompositingQuality = CompositingQuality.HighQuality;

						// An unwanted border appears when using InterpolationMode.HighQualityBicubic to resize the image
						// as the algorithm appears to be pulling averaging detail from surrounding pixels beyond the edge 
						// of the image. Using the ImageAttributes class to specify that the pixels beyond are simply mirror 
						// images of the pixels within solves this problem.
						using (ImageAttributes wrapMode = new ImageAttributes())
						{
							wrapMode.SetWrapMode(WrapMode.TileFlipXY);
							graphics.DrawImage(
								image,
								new Rectangle(0, 0, rectangle.Width, rectangle.Height),
								rectangle.X,
								rectangle.Y,
								rectangle.Width,
								rectangle.Height,
								GraphicsUnit.Pixel,
								wrapMode);
						}
					}

					// Reassign the image.
					image.Dispose();
					image = newImage;
				}
			}
			catch (Exception ex)
			{
				if (newImage != null)
				{
					newImage.Dispose();
				}

				throw new ImageProcessingException("Error processing image with crop", ex);
			}

			return (Bitmap)image;
		}
	}
}

