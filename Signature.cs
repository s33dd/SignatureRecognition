using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;

namespace SignatureRecognition {
	public class Signature {
		public int DistCount { get; set; }
		[JsonIgnore]
		public StrokeCollection Strokes { get; set; }
		public List<StylusPointCollection> Points { get; set; }
		public double AveragePressure { get; set; }
		public int StrokesQuantity { get; set; }
		public double HeightWidthRatio { get; set; }
		public int PointsQuantity { get; set; }
		public string? Owner { get; set; }
		public List<double> Distances { get; set; }

		public Signature(StrokeCollection sc) {
			Distances = new List<double>();
			Points = new List<StylusPointCollection>();
			Strokes = sc;
			AveragePressure = 0;
			int pointsQuantity = 0;
			foreach (Stroke stroke in Strokes) {
				foreach (var point in stroke.StylusPoints) {
					pointsQuantity++;
					AveragePressure += point.PressureFactor;
				}
			}
			DistCount = pointsQuantity / 2;
			int step = pointsQuantity / DistCount;
			StylusPointCollection points = new StylusPointCollection();
			for (int i = 0; i < Strokes.Count; i++) {
				points.Add(Strokes[i].StylusPoints.Clone());
			}
			for (int j = step; j < points.Count; j += step) {
				StylusPoint point1 = points[j];
				StylusPoint point2 = points[j - step];
				double distance = Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
				distance /= Recognizer.GetBounds(Strokes).Width;
				Distances.Add(distance);
			}
			AveragePressure /= pointsQuantity;
			StrokesQuantity = Strokes.Count;
			PointsQuantity = pointsQuantity;
			var bounds = Recognizer.GetBounds(Strokes);
			HeightWidthRatio = Math.Round(bounds.Height / bounds.Width, 3);
			foreach (Stroke stroke in Strokes) {
				Points.Add(stroke.StylusPoints.Clone());
			}
		}

		[JsonConstructor]
		public Signature(List<StylusPointCollection> points) {
			Points = points;
			Strokes = new StrokeCollection();
			foreach (var set in points) {
				Strokes.Add(new Stroke(set));
			}
			AveragePressure = 0;
			int pointsQuantity = 0;
			foreach (Stroke stroke in Strokes) {
				foreach (var point in stroke.StylusPoints) {
					pointsQuantity++;
					AveragePressure += point.PressureFactor;
				}
			}
			Distances = new List<double>();
			DistCount = pointsQuantity / 2;
			int step = pointsQuantity / DistCount;
			for (int i = 0; i < points.Count; i++) {
				for (int j = step; j < points.Count; j += step) {
					StylusPoint point1 = points[i][j];
					StylusPoint point2 = points[i][j - step];
					double distance = Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
					distance /= Recognizer.GetBounds(Strokes).Width;
					Distances.Add(distance);
				}
			}
			AveragePressure /= pointsQuantity;
			PointsQuantity = pointsQuantity;
			StrokesQuantity = Strokes.Count;
			var bounds = Recognizer.GetBounds(Strokes);
			HeightWidthRatio = Math.Round(bounds.Height / bounds.Width, 3);
			foreach (Stroke stroke in Strokes) {
				Points.Add(stroke.StylusPoints.Clone());
			}
		}
	}
}
