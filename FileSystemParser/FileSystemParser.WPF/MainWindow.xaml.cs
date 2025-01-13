using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileSystemParser.IPC;
using System.Text.Json;
using System;

namespace FileSystemParser.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string _path = string.Empty;
		private string _checkInterval = string.Empty;
		private string _maximumConcurrentProcessing = string.Empty;

		public MainWindow()
		{
			InitializeComponent();

			IpcProvider.Initialize();
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var textBox = sender as TextBox;
			e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			var folderDialog = new OpenFolderDialog();

			if (folderDialog.ShowDialog() != true)
			{
				return;
			}

			SelectPathTextBox.Text = folderDialog.FolderName;
			_path = folderDialog.FolderName;
		}

		private void CheckIntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			_checkInterval = CheckIntervalTextBox.Text;
		}

		private void MaximumConcurrentProcessingTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			_maximumConcurrentProcessing = MaximumConcurrentProcessingTextBox.Text;
		}

		private void StartButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				IpcProvider.WriteMessage(JsonSerializer.Serialize(
					new IpcMessage
					{
						Path = _path,
						CheckInterval = int.Parse(_checkInterval),
						MaximumConcurrentProcessing = int.Parse(_maximumConcurrentProcessing)
					}));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
	}
}
