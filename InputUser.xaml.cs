using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SignatureRecognition {
	/// <summary>
	/// Interaction logic for InputUser.xaml
	/// </summary>
	public partial class InputUser : Window {
		public InputUser() {
			InitializeComponent();
		}

		private void Accept_Click(object sender, RoutedEventArgs e) {
			this.DialogResult = true;
		}

		public string User {
			get {
				return userBox.Text;
			}
		}
	}
}
