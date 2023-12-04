using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace SignatureRecognition {
	public static class Recognizer {
		private static double normalSize = 100.0;

		//Returns index of the most left stroke in collection
		private static int FindMostLeftStroke(StrokeCollection strokes) {
			List<double> xCoords = new List<double>();
			foreach (var stroke in strokes) {
				xCoords.Add(stroke.GetBounds().X);
			}
			return xCoords.IndexOf(xCoords.Min());
		}

		//Returns index of the most top stroke in collection
		private static int FindMostTopStroke(StrokeCollection strokes) {
			List<double> yCoords = new List<double>();
			foreach (var stroke in strokes) {
				yCoords.Add(stroke.GetBounds().Y);
			}
			return yCoords.IndexOf(yCoords.Min());
		}

		public static StrokeCollection Normalize(StrokeCollection strokes) {
			/* Transformation matrix [a b
									  c d
									  f e]
			a, d -- for scaling, a - horizontal scaling, d - vertical scaling
			b, c -- rotating, (b - vertical rotation, c - horizontal rotation
			f, e -- axis offset (f - axis x, e - axis y */

			//Move signature to start of canvas

			double[] xOffsets = new double[strokes.Count];
			double[] yOffsets = new double[strokes.Count];
			//XOffset for the leftmost stroke
			int leftmost = FindMostLeftStroke(strokes);
			xOffsets[leftmost] = -strokes[leftmost].GetBounds().X;
			//YOffset for the topmost stroke
			int topmost = FindMostTopStroke(strokes);
			yOffsets[topmost] = -strokes[topmost].GetBounds().Y;

			//Calculating strokes offsets relatively to topmost & leftmost
			for (int i = 0; i < strokes.Count; i++) {
				var strokeBounds = strokes[i].GetBounds();
				double newX;
				double newY;
				if (i == leftmost) {
					newX = 0;
				} else {
					newX = strokeBounds.X - strokes[leftmost].GetBounds().X;
				}
				if (i == topmost) {
					newY = 0;
				} else {
					newY = strokeBounds.Y - strokes[topmost].GetBounds().Y;
				}
				xOffsets[i] = -(strokeBounds.X - newX);
				yOffsets[i] = -(strokeBounds.Y - newY);
			}
			//Moving
			for (int i = 0; i < strokes.Count; i++) {
				Matrix normalizationMatrix = new Matrix(1, 0,
														0, 1,
														xOffsets[i], yOffsets[i]);
				strokes[i].Transform(normalizationMatrix, false);
			}
			return strokes;
		}
	}
}
