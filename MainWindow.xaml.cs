using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
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

		private string user;
		private const int quantity = 3;
		private int currentInput;
		private List<Signature> inputs;
		private const string filePath = @"./json";
		public ObservableCollection<string> Users { get; set; }
		public MainWindow() {
			InitializeComponent();
			Users = new ObservableCollection<string>();
			LoadUsers();
			LoadBox.ItemsSource = Users;
		}

		private void LoadUsers() {
			string listFile = filePath + @"/list.json";
			if (File.Exists(listFile)) {
				string jsonList = File.ReadAllText(listFile);
				Users = JsonConvert.DeserializeObject<ObservableCollection<string>>(jsonList);
			}
		}
		private void GetSigBtn_Click(object sender, RoutedEventArgs e) {
			inputs = new List<Signature>();
			InputUser userWindow = new InputUser();
			if (userWindow.ShowDialog() == true) {
				if (userWindow.User != string.Empty) {
					string listFile = filePath + @"/list.json";
					List<string> saved = new List<string>();
					if (File.Exists(listFile)) {
						string jsonList = File.ReadAllText(listFile);
						saved = JsonConvert.DeserializeObject<List<string>>(jsonList);
						if (saved.Contains(userWindow.User.Replace(' ', '_'))) {
							MessageBox.Show("Такая подпись уже сохранена", "Ошибка");
							return;
						}
					}
					user = userWindow.User;
				} else {
					MessageBox.Show("Проверьте введённые данные.", "Ошибка");
					return;
				}
			}
			currentInput = 0;
			MessageBox.Show($"Введите подпись в поле {quantity} раз.", "Сохранение");
			SaveBtn.IsEnabled = true;
			GetSigBtn.IsEnabled = false;
		}

		private void ClearBtn_Click(object sender, RoutedEventArgs e) {
			if (currentInput > 1) {
				MessageBox.Show("Попытки ввода сброшены.", "Сохранение");
				GetSigBtn.IsEnabled = true;
			}
			currentInput = 0;
			Canvas.Strokes.Clear();
		}

		private void RecBtn_Click(object sender, RoutedEventArgs e) {
			if (Canvas.Strokes.Count == 0) {
				MessageBox.Show("Введите подпись", "Ошибка");
				return;
			}
			string listFile = filePath + @"/list.json";
			if (!File.Exists(listFile)) {
				MessageBox.Show("Нет сохранённых пользователей.", "Ошибка");
				return;
			}
			Signature subject = new Signature(Recognizer.Normalize(Canvas.Strokes.Clone()));
			foreach (string name in Users) {
				string file = @$"{filePath}/{name}.json";
				if (!File.Exists(file)) {
					continue;
				}
				string json = File.ReadAllText(file);
				Signature compared = JsonConvert.DeserializeObject<Signature>(json);
				const double angle = 5.0;
				int quantity = (int)Math.Round(360 / angle);
				bool isSame = Recognizer.Compare(subject, compared);
				if (isSame) {
					MessageBox.Show($"Подпись принадлежит {compared.Owner}", "Распознавание");
					return;
				}
				for (int i = 0; i < quantity; i++) {
					subject.Strokes = Recognizer.Normalize(Recognizer.Rotate(subject.Strokes, angle));
					isSame = Recognizer.Compare(subject, compared);
					if (isSame) {
						MessageBox.Show($"Подпись принадлежит {compared.Owner}", "Распознавание");
						return;
					}
				}
			}
			MessageBox.Show("Совпадений не найдено.", "Распознавание");
		}
		private void SaveBtn_Click(object sender, RoutedEventArgs e) {
			if (currentInput > 0) {
				for (int i = 0; i < currentInput; i++) {
					if (Canvas.Strokes.Count != inputs[i].StrokesQuantity) {
						Canvas.Strokes.Clear();
						MessageBox.Show("Возможно, подпись сильно отличается от прошлых", "Ошибка");
						return;
					}
				}
			}
			Signature current = new Signature(Recognizer.Normalize(Canvas.Strokes.Clone()));
			inputs.Add(current);
			currentInput++;
			if (currentInput < quantity) {
				Canvas.Strokes.Clear();
				MessageBox.Show($"Осталось {quantity - currentInput}", "Сохранение");
			} else {
				if (!Directory.Exists(filePath)) {
					Directory.CreateDirectory(filePath);
				}
				int[] minPoints = new int[current.StrokesQuantity];
				for (int i = 0; i < current.StrokesQuantity; i++) {
					minPoints[i] = current.Strokes[i].StylusPoints.Count();
				}
				//Save minimal points quantity
				foreach (var sig in inputs) {
					for (int i = 0; i < sig.StrokesQuantity; i++) {
						if (minPoints[i] > sig.Strokes[i].StylusPoints.Count()) {
							minPoints[i] = sig.Strokes[i].StylusPoints.Count();
						}
					}
				}
				List<StylusPointCollection> pointsforStrokes = new List<StylusPointCollection>(current.StrokesQuantity);
				List<float[]> pressureForStrokes = new List<float[]>(current.StrokesQuantity);
				for (int i = 0; i < pointsforStrokes.Capacity; i++) {
					StylusPointCollection points = new StylusPointCollection(new StylusPoint[minPoints[i]]);
					float[] pressures = new float[minPoints[i]];
					pressureForStrokes.Add(pressures);
					pointsforStrokes.Add(points);
				}
				foreach (var sig in inputs) {
					for (int i = 0; i < pointsforStrokes.Count; i++) {
						var localPoints = sig.Strokes[i].StylusPoints.Clone();
						for (int j = 0; j < minPoints[i]; j++) {
							double x = pointsforStrokes[i][j].X;
							double y = pointsforStrokes[i][j].Y;
							float pressure = pointsforStrokes[i][j].PressureFactor;
							x += localPoints[j].X;
							y += localPoints[j].Y;
							pressure += localPoints[j].PressureFactor;
							pressureForStrokes[i][j] = pressure;
							pointsforStrokes[i][j] = new StylusPoint(x, y);
						}
					}
				}
				for (int j = 0; j < pointsforStrokes.Count; j++) {
					for (int i = 0; i < pointsforStrokes[j].Count; i++) {
						double x = pointsforStrokes[j][i].X; 
						double y = pointsforStrokes[j][i].Y;
						float pressure = pressureForStrokes[j][i];
						x /= quantity;
						y /= quantity;
						pressure /= quantity;
						pointsforStrokes[j][i] = new StylusPoint(x, y, pressure);
					}
				}
				StrokeCollection sc = new StrokeCollection();
				foreach (var points in pointsforStrokes) {
					sc.Add(new Stroke(points));
				}
				Signature finalSig = new Signature(sc);
				finalSig.Owner = user;
				string json = JsonConvert.SerializeObject(finalSig, Formatting.Indented);
				string fileName = filePath + @$"\{user.Replace(' ','_')}.json";
				using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate)) {
					byte[] buf = Encoding.Default.GetBytes(json);
					fs.Write(buf, 0, buf.Length);
				}
				List<string> savedSigs = new List<string>();
				string listFile = filePath + @"/list.json";
				if (File.Exists(listFile)) {
					string jsonList = File.ReadAllText(listFile);
					savedSigs = JsonConvert.DeserializeObject<List<string>>(jsonList);
					savedSigs.Add($"{user.Replace(' ', '_')}");
					jsonList = JsonConvert.SerializeObject(savedSigs, Formatting.Indented);
					using (FileStream fs = new FileStream(listFile, FileMode.OpenOrCreate)) {
						byte[] buf = Encoding.Default.GetBytes(jsonList);
						fs.Write(buf, 0, buf.Length);
					}
				} else {
					savedSigs.Add($"{user.Replace(' ', '_')}");
					string jsonList = JsonConvert.SerializeObject(savedSigs, Formatting.Indented);
					using (FileStream fs = new FileStream(listFile, FileMode.OpenOrCreate)) {
						byte[] buf = Encoding.Default.GetBytes(jsonList);
						fs.Write(buf, 0, buf.Length);
					}
				}

				Users.Add(user.Replace(' ', '_'));
				LoadBox.ItemsSource = Users;

				currentInput = 0;
				MessageBox.Show("Сохранение завершено.", "Сохранение");
				SaveBtn.IsEnabled = false;
				GetSigBtn.IsEnabled = true;
				Canvas.Strokes.Clear();
			}
		}

		private void LoadBtn_Click(object sender, RoutedEventArgs e) {
			string path = filePath + $@"/{LoadBox.SelectedItem.ToString()}.json";
			string json = File.ReadAllText(path);
			Signature sig = JsonConvert.DeserializeObject<Signature>(json);
			Canvas.Strokes = sig.Strokes;
		}
	}
}
