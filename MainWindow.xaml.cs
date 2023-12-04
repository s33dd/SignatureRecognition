using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace SignatureRecognition {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		//Strokes, GetGeometry, StylusPoints
		public MainWindow() {
			InitializeComponent();
		}

		private void GetStrokesBtn_Click(object sender, RoutedEventArgs e) {
			var strokes = Canvas.Strokes;
			foreach (var item in strokes) {
				var points = item.StylusPoints;
			}
		}

		private void ClearBtn_Click(object sender, RoutedEventArgs e) {
			Canvas.Strokes.Clear();
		}

		private void MoveBtn_Click(object sender, RoutedEventArgs e) {
			Canvas.Strokes = Recognizer.Normalize(Canvas.Strokes);
		}
	}
}
