using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
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

			//Scaling

			//Merge strokes fo proper scaling
			StylusPointCollection sp = new StylusPointCollection();
			List<int> endIndices = new List<int>();
			List<int> startIndices = new List<int>(); //Arrays for saving indices for each stroke for separation
			endIndices.Clear();
			startIndices.Clear();
			for (int i = 0; i < strokes.Count; i++) {
				if (i == 0) {
					startIndices.Add(i);
					endIndices.Add(strokes[i].StylusPoints.Count - 1);
				} else {
					startIndices.Add(endIndices[i - 1] + 1);
					endIndices.Add(endIndices[i - 1] + strokes[i].StylusPoints.Count);
				}
				var stylusPoints = strokes[i].StylusPoints.Clone();
				sp.Add(stylusPoints);
			}

			strokes.Clear();
			strokes.Add(new Stroke(sp));

			var bounds = strokes[0].GetBounds();
			double xCenter = bounds.Left + bounds.Width / 2;
			double yCenter = bounds.Top + bounds.Height / 2;
			double verticalScaleMultiplier = bounds.Height / normalSize;
			double horizontalScaleMultiplier = bounds.Width / normalSize;
			double scaleMultiplier = 1 / Math.Max(verticalScaleMultiplier, horizontalScaleMultiplier);
			Matrix normalizationMatrix = Matrix.Identity;
			normalizationMatrix.Scale(scaleMultiplier, scaleMultiplier);
			strokes[0].Transform(normalizationMatrix, false);

			//Save center position
			bounds = strokes[0].GetBounds();
			double xNewCenter = bounds.Left + bounds.Width / 2;
			double yNewCenter = bounds.Top + bounds.Height / 2;
			double xOffset = Math.Abs(xCenter - xNewCenter);
			double yOffset = Math.Abs(yCenter - yNewCenter);
			normalizationMatrix = Matrix.Identity;
			switch (scaleMultiplier < 1) {
				case true:
					normalizationMatrix.Translate(xOffset, yOffset);
					break;
				case false:
					normalizationMatrix.Translate(-xOffset, -yOffset);
					break;
			}
			strokes[0].Transform(normalizationMatrix, false);

			//Separate one stroke to several strokes as before
			StrokeCollection newStrokes = new StrokeCollection();
			var points = strokes[0].StylusPoints.Clone();
			for (int i = 0; i < startIndices.Count; i++) {
				StylusPointCollection newPoints = new StylusPointCollection();
				for (int j = startIndices[i]; j <= endIndices[i]; j++) {
					newPoints.Add(points[j]);
				}
				Stroke str = new Stroke(newPoints);
				newStrokes.Add(str);
			}

			strokes = newStrokes;

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
				normalizationMatrix = Matrix.Identity;
				normalizationMatrix.Translate(xOffsets[i], yOffsets[i]);
				strokes[i].Transform(normalizationMatrix, false);
			}
			return strokes;
		}

		public static StrokeCollection Rotate(StrokeCollection strokes) {
			StylusPointCollection sp = new StylusPointCollection();
			List<int> endIndices = new List<int>();
			List<int> startIndices = new List<int>(); //Arrays for saving indices for each stroke for separation
			endIndices.Clear();
			startIndices.Clear();
			for (int i = 0; i < strokes.Count; i++) {
				if (i == 0) {
					startIndices.Add(i);
					endIndices.Add(strokes[i].StylusPoints.Count - 1);
				} else {
					startIndices.Add(endIndices[i - 1] + 1);
					endIndices.Add(endIndices[i - 1] + strokes[i].StylusPoints.Count);
				}
				var stylusPoints = strokes[i].StylusPoints.Clone();
				sp.Add(stylusPoints);
			}

			strokes.Clear();
			strokes.Add(new Stroke(sp));

			Matrix matrix = Matrix.Identity;
			foreach (var stroke in strokes) {
				var bounds = stroke.GetBounds();
				double xCenter = bounds.Left + bounds.Width / 2;
				double yCenter = bounds.Top + bounds.Height / 2;
				matrix.RotateAt(15, xCenter, yCenter);
				stroke.Transform(matrix, false);
			}

			StrokeCollection newStrokes = new StrokeCollection();
			var points = strokes[0].StylusPoints.Clone();
			for (int i = 0; i < startIndices.Count; i++) {
				StylusPointCollection newPoints = new StylusPointCollection();
				for (int j = startIndices[i]; j <= endIndices[i]; j++) {
					newPoints.Add(points[j]);
				}
				Stroke str = new Stroke(newPoints);
				newStrokes.Add(str);
			}

			strokes = newStrokes;
			return strokes;
		}
	}
}
