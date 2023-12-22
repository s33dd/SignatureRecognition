﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

		public static StrokeCollection Rotate(StrokeCollection strokes, double angle) {
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
				matrix.RotateAt(angle, xCenter, yCenter);
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

		public static Rect GetBounds(StrokeCollection strokes) {
			StylusPointCollection sp = new StylusPointCollection();
			for (int i = 0; i < strokes.Count; i++) {
				var stylusPoints = strokes[i].StylusPoints.Clone();
				sp.Add(stylusPoints);
			}
			Stroke str = new Stroke(sp);
			return str.GetBounds();
		}

		public static StrokeCollection NoiseReduction(StrokeCollection strokes) {
			StylusPointCollection sp = new StylusPointCollection();
			const double maximalDistance = 1;
			for (int i = 0; i < strokes.Count; i++) {
				var stylusPoints = strokes[i].StylusPoints.Clone().ToArray();
				IComparer<StylusPoint> comparer = new XComparer();
				Array.Sort(stylusPoints, comparer);
				for (int j = 1; i < stylusPoints.Length; i++) {
					if (stylusPoints[j].X - stylusPoints[j-1].X > maximalDistance) {
						strokes[i].StylusPoints.Remove(stylusPoints[j]);
					}
				}
				comparer = new YComparer();
				Array.Sort(stylusPoints, comparer);
				for (int j = 1; i < stylusPoints.Length; i++) {
					if (stylusPoints[j].Y - stylusPoints[j - 1].Y > maximalDistance) {
						strokes[i].StylusPoints.Remove(stylusPoints[j]);
					}
				}
			}
			return strokes;
		}

		public static bool Compare(Signature first, Signature second) {
			const double ratioEpsil = 0.3;
			const double pointEpsil = 15;
			const double pointsEpsil = 100;
			const double pressureEpsil = 0.1;
			bool result = false;
			if (first.StrokesQuantity != second.StrokesQuantity) {
				return result;
			}
			if (Math.Abs(second.AveragePressure - first.AveragePressure) > pressureEpsil) {
				return result;
			}
			if (Math.Abs(second.HeightWidthRatio - first.HeightWidthRatio) > ratioEpsil) {
				return result;
			}
			if (Math.Abs(first.PointsQuantity - second.PointsQuantity) > pointsEpsil) {
				return false;
			}
			int count = 0;
			for (int i = 0; i < second.StrokesQuantity; i++) {
				int minQuantity = Math.Min(first.Strokes[i].StylusPoints.Count, second.Strokes[i].StylusPoints.Count);
				for (int j = 0; j < minQuantity; j++) {
					double xDisp = Math.Abs(first.Strokes[i].StylusPoints[j].X - second.Strokes[i].StylusPoints[j].X);
					double yDisp = Math.Abs(first.Strokes[i].StylusPoints[j].Y - second.Strokes[i].StylusPoints[j].Y);
					if (xDisp < pointEpsil & yDisp < pointEpsil) {
						count++;
					}
				}
			}
			double estimated = second.PointsQuantity * 0.7; //70% of all points
			if (count >= estimated) {
				result = true;
			}
			return result;
		}
	}
	class XComparer : IComparer<StylusPoint> {
		public int Compare(StylusPoint first, StylusPoint second) {
			if (first.X  < second.X) {
				return 1;
			} else if (first.X > second.X) {
				return -1;
			} else {
				return 0;
			}
		}
	}
	class YComparer : IComparer<StylusPoint> {
		public int Compare(StylusPoint first, StylusPoint second) {
			if (first.Y < second.Y) {
				return 1;
			} else if (first.X > second.X) {
				return -1;
			} else {
				return 0;
			}
		}
	}
}
