using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;

namespace SignatureRecognition {
	public class Signature {
		[JsonIgnore]
		public StrokeCollection Strokes { get; set; }
		public List<StylusPointCollection> Points { get; set; }
		public double AveragePressure { get; set; }
		public int StrokesQuantity { get; set; }
		public double HeightWidthRatio { get; set; }
		public string? Owner { get; set; }

		public Signature(StrokeCollection sc) {
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
			AveragePressure /= pointsQuantity;
			StrokesQuantity = Strokes.Count;
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
			AveragePressure /= pointsQuantity;
			StrokesQuantity = Strokes.Count;
			var bounds = Recognizer.GetBounds(Strokes);
			HeightWidthRatio = Math.Round(bounds.Height / bounds.Width, 3);
			foreach (Stroke stroke in Strokes) {
				Points.Add(stroke.StylusPoints.Clone());
			}
		}
	}
}
