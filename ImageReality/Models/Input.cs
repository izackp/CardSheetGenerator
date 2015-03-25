using System;
using FullSerializer;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace ImageReality
{
	public class Input
	{
		[fsProperty("images")]
		public List<string> Images;

		[fsProperty("cardWidth")]
		public double CardWidth; //Inches

		[fsProperty("cardHeight")]
		public double CardHeight; //Inches

		[fsProperty("dpi")]
		public double DPI;

		[fsProperty("guideLineSize")]
		public double GuideLineSize;

		public List<string> GenerateCardSheets() {
			int cardPxWidth = (int)(CardWidth * DPI);
			int cardPxHeight = (int)(CardHeight * DPI);

			List<Image> decodedImages = DecodeImages ();
			for (int i = 0; i < decodedImages.Count; i += 1) {
				Image image = decodedImages [i];
				image = image.Trim ();
				image = image.Resize (cardPxWidth, cardPxHeight);
				image = image.Extent (cardPxWidth, cardPxHeight, Color.White);
				if (GuideLineSize != 0) {
					DrawableLine[] lines = GenerateGuideLines ();
					image = image.DrawLines (lines);
				}
				decodedImages [i] = image;
			}

			List<Image> imageSheets = GenerateMontage (decodedImages);
			List<string> base64ImageSheets = new List<string> ();
			int count = 0;
			foreach (Image imageSheet in imageSheets) {
				MemoryStream stream = new MemoryStream ();
				imageSheet.Save ("test" + count + ".png");
				count += 1;
				imageSheet.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
				byte[] imageBytes = stream.ToArray ();
				string result = Convert.ToBase64String (imageBytes);
				base64ImageSheets.Add (result);
			}
			return base64ImageSheets;
		}


		public List<Image> DecodeImages() {
			List<Image> resultImages = new List<Image> ();
			foreach (string image in Images) {
				byte[] imageBytes = Convert.FromBase64String (image);
				MemoryStream ms = new MemoryStream (imageBytes, 0, imageBytes.Length);
				ms.Write (imageBytes, 0, imageBytes.Length);
				Image img = Image.FromStream (ms);
				if (img != null)
					resultImages.Add (img);
			}
			return resultImages;
		}

		List<Image> GenerateMontage(List<Image> decodedImages) {
			List<Image> imageSheets = new List<Image> ();

			for (int i = 0; i < decodedImages.Count; i += 9) {
				int numImages = decodedImages.Count - i;
				if (numImages > 9)
					numImages = 9;
				List<Image> setOf9 = decodedImages.GetRange (i, numImages);
				Image result = ImageExt.TileImages (setOf9, 3);
				imageSheets.Add (result);
			}
			return imageSheets;
		}

		DrawableLine[] GenerateGuideLines() {
			int cardPxWidth = (int)(CardWidth * DPI);
			int cardPxHeight = (int)(CardHeight * DPI);

			int cardPxGuideRight = cardPxWidth - 1;
			int cardPxGuideBottom = cardPxHeight - 1;
			int guideLinePxSize = (int)(GuideLineSize * DPI);

			DrawableLine topLeftVertLine = new DrawableLine(0, 0, 0, 0);
			topLeftVertLine.End.Y = guideLinePxSize;

			DrawableLine topLeftHorLine = new DrawableLine(0, 0, 0, 0);
			topLeftHorLine.End.X = guideLinePxSize;

			DrawableLine topRightVertLine = new DrawableLine(0, 0, 0, 0);
			topRightVertLine.Start.X = cardPxGuideRight;
			topRightVertLine.End.X = cardPxGuideRight;
			topRightVertLine.End.Y = guideLinePxSize;

			DrawableLine topRightHorLine = new DrawableLine(0, 0, 0, 0);
			topRightHorLine.Start.X = cardPxGuideRight;
			topRightHorLine.End.X = cardPxGuideRight - guideLinePxSize;

			DrawableLine bottomLeftVertLine = new DrawableLine(0, 0, 0, 0);
			bottomLeftVertLine.Start.Y = cardPxGuideBottom;
			bottomLeftVertLine.End.Y = cardPxGuideBottom-guideLinePxSize;

			DrawableLine bottomLeftHorLine = new DrawableLine(0, 0, 0, 0);
			bottomLeftHorLine.Start.Y = cardPxGuideBottom;
			bottomLeftHorLine.End.X = guideLinePxSize;
			bottomLeftHorLine.End.Y = cardPxGuideBottom;

			DrawableLine bottomRightVertLine = new DrawableLine(0, 0, 0, 0);
			bottomRightVertLine.Start.Y = cardPxGuideBottom;
			bottomRightVertLine.Start.X = cardPxGuideRight;
			bottomRightVertLine.End.X = cardPxGuideRight;
			bottomRightVertLine.End.Y = cardPxGuideBottom-guideLinePxSize;

			DrawableLine bottomRightHorLine = new DrawableLine(0, 0, 0, 0);
			bottomRightHorLine.Start.Y = cardPxGuideBottom;
			bottomRightHorLine.Start.X = cardPxGuideRight;
			bottomRightHorLine.End.X = cardPxGuideRight-guideLinePxSize;
			bottomRightHorLine.End.Y = cardPxGuideBottom;

			return new [] {
				topRightHorLine,
				topRightVertLine,
				topLeftHorLine,
				topLeftVertLine,
				topRightVertLine,
				topRightHorLine,
				bottomLeftHorLine,
				bottomLeftVertLine,
				bottomRightHorLine,
				bottomRightVertLine
			};
		}
	}
}

