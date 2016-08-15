using System;

namespace ImageReality
{
	public enum DespeckleFilterType {
		Adaptive,
		Recursive
	}

	public struct PixelPos
	{
		public int x;
		public int y;
		public PixelPos(int xx, int yy) {
			x = xx;
			y = yy;
		}

		public PixelPos() {
			x = 0;
			y = 0;
		}
	}
		
	public struct PixelsList {
		public const int MAX_RADIUS = 30;
		public const int MAX_LIST_ELEMS = ((2 * MAX_RADIUS + 1) * (2 * MAX_RADIUS + 1));

		PixelPos[]   elems;
		int          start;
		int          count;

		public PixelsList() {
			elems = new PixelPos[MAX_LIST_ELEMS];
			start = 0;
			count = 0;
		}

		public void Clean() {
			count = 0;
		}

		public void AddElem (PixelPos elem)
		{
			count++;
			int pos = start + count;
			if (pos >= MAX_LIST_ELEMS)
				pos = pos - MAX_LIST_ELEMS;

			elems[pos] = elem; //elem == src + pos
		}

		//Appears to delete the element in the front of the list
		public void DelElem ()
		{
			count--;
			start++;

			if (start >= MAX_LIST_ELEMS)
				start = 0;
		}

		private static readonly Random Random = new Random();
		public PixelPos GetRandomElem ()
		{
			int pos = Random.Next(start, start + count);

			if (pos >= MAX_LIST_ELEMS)
				pos -= MAX_LIST_ELEMS;

			return elems[pos];
		}
	}

	public class DespeckleHistogram
	{
		public int[]        elems = new int[256]; /* Number of pixels that fall into each luma bucket */
		public PixelsList[] origs = new PixelsList[256]; /* Original pixels */
		public int			xmin;
		public int			ymin;
		public int			xmax;
		public int			ymax; /* Source rect */

		public int			hist0;    /* Less than min treshold */
		public int			hist255;  /* More than max treshold */
		public int			histrest; /* From min to max        */

		//Input
		public int 			BlackLevel = 7;
		public int 			WhiteLevel = 248;

		public void Add(ushort pixelLuminance, PixelPos orig)
		{
			elems [pixelLuminance]++;
			origs [pixelLuminance].AddElem (orig);
		}

		public void Remove(ushort pixelLuminance)
		{
			elems [pixelLuminance]--;
			origs [pixelLuminance].DelElem ();
		}

		public void Clean()
		{
			for (int i = 0; i < 256; i++)
			{
				elems [i] = 0;
				origs [i].Clean ();
			}
		}

		public PixelPos GetMedian (PixelPos _default)
		{
			int count = histrest;
			int i;
			int sum = 0;

			if (count == 0)
				return _default;

			count = (count + 1) / 2;

			i = 0;
			while ((sum += elems[i]) < count)
				i++;

			return origs [i].GetRandomElem ();
		}

		public void AddVal (Pixel[,] src, int x, int y)
		{
			ushort pixelLuminance = (ushort)src[x,y].RGBLuminance();

			if (pixelLuminance > BlackLevel && pixelLuminance < WhiteLevel)
			{
				Add (pixelLuminance, new PixelPos(x, y));
				histrest++;
			}
			else
			{
				if (pixelLuminance <= BlackLevel)
					hist0++;

				if (pixelLuminance >= WhiteLevel)
					hist255++;
			}
		}

		public void DelVal (Pixel[,] src, int x, int y)
		{
			ushort pixelLuminance = (ushort)src[x,y].RGBLuminance();

			if (pixelLuminance > BlackLevel && pixelLuminance < WhiteLevel)
			{
				Remove (pixelLuminance);
				histrest--;
			}
			else
			{
				if (pixelLuminance <= BlackLevel)
					hist0--;

				if (pixelLuminance >= WhiteLevel)
					hist255--;
			}
		}

		public void AddVals (Pixel[,] src, int xmin, int ymin, int xmax, int ymax)
		{
			if (xmin > xmax)
				return;

			for (int y = ymin; y <= ymax; y++)
			{
				for (int x = xmin; x <= xmax; x++)
				{
					AddVal (src, x, y);
				}
			}
		}

		public void DelVals (Pixel[,] src, int xmin, int ymin, int xmax, int ymax)
		{
			if (xmin > xmax)
				return;

			for (int y = ymin; y <= ymax; y++)
			{
				for (int x = xmin; x <= xmax; x++)
				{
					DelVal (src, x, y);
				}
			}
		}

		public void Update (Pixel[,] src, int xmin, int ymin, int xmax, int ymax)
		{
			/* assuming that radious of the box can change no more than one
     pixel in each call */
			/* assuming that box is moving either right or down */

			DelVals (src, this.xmin, this.ymin, xmin - 1, this.ymax);
			DelVals (src, xmin, this.ymin, xmax, ymin - 1);
			DelVals (src, xmin, ymax + 1, xmax, this.ymax);

			AddVals (src, this.xmax + 1, ymin, xmax, ymax);
			AddVals (src, xmin, ymin, this.xmax, this.ymin - 1);
			AddVals (src, this.xmin, this.ymax + 1, this.xmax, ymax);

			this.xmin = xmin;
			this.ymin = ymin;
			this.xmax = xmax;
			this.ymax = ymax;
		}

		static public Pixel[,] DespeckleMedian (Pixel[,] src, int radius = 3, DespeckleFilterType FilterType = DespeckleFilterType.Adaptive, int blackLevel = 7, int whiteLevel = 248)
		{
			int width = src.GetLength (0);
			int height = src.GetLength (1);
			int adapt_radius = radius;

			Pixel[,] dst = new Pixel[src.GetLength (0), src.GetLength (1)];
			DespeckleHistogram histogram = new DespeckleHistogram ();
			histogram.BlackLevel = blackLevel;
			histogram.WhiteLevel = whiteLevel;

			for (int y = 0; y < height; y++)
			{
				int x = 0;
				int ymin = Math.Max (0, y - adapt_radius);
				int ymax = Math.Min (height - 1, y + adapt_radius);
				int xmin = Math.Max (0, x - adapt_radius);
				int xmax = Math.Min (width - 1, x + adapt_radius);
				histogram.hist0    = 0;
				histogram.histrest = 0;
				histogram.hist255  = 0;
				histogram.Clean();
				histogram.xmin = xmin;
				histogram.ymin = ymin;
				histogram.xmax = xmax;
				histogram.ymax = ymax;
				histogram.AddVals(src, xmin, ymin, xmax, ymax);

				for (x = 0; x < width; x++)
				{
					ymin = Math.Max (0, y - adapt_radius); /* update ymin, ymax when adapt_radius changed (FILTER_ADAPTIVE) */
					ymax = Math.Min (height - 1, y + adapt_radius);
					xmin = Math.Max (0, x - adapt_radius);
					xmax = Math.Min (width - 1, x + adapt_radius);

					histogram.Update (src, xmin, ymin, xmax, ymax);

					PixelPos medianPixelPos = histogram.GetMedian (new PixelPos(x, y));

					if (FilterType == DespeckleFilterType.Recursive)
					{
						histogram.DelVal (src, x, y);
						src [x, y] = src [medianPixelPos.x, medianPixelPos.y];
						histogram.AddVal (src, x, y);
					}

					dst [x, y] = src [medianPixelPos.x, medianPixelPos.y];

					/*
           * Check the histogram and adjust the diameter accordingly...
           */
					if (FilterType == DespeckleFilterType.Adaptive)
					{
						if (histogram.hist0 >= adapt_radius || histogram.hist255 >= adapt_radius)
						{
							if (adapt_radius < radius)
								adapt_radius++;
						}
						else if (adapt_radius > 1)
						{
							adapt_radius--;
						}
					}
				}
			}

			return dst;
		}
	}
}

