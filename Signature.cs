using System;
using System.Windows.Ink;

namespace SignatureRecognition {
	public class Signature {
		public StrokeCollection Strokes { get; set; }
		public double AveragePressure { get; set; }
		public int StrokesQuantity { get; set; }
		public double HeightWidthRatio { get; set; }
		public string? Owner { get; set; }

		public Signature(StrokeCollection sc) { 
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
		}
	}
}
