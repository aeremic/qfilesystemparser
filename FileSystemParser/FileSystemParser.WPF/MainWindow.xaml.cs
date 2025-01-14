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
        private string _path = @"C:\Users\Andrija\qfilesystemparser\FileSystemParser\Files";
        private string _checkInterval = "1000";
        private string _maximumConcurrentProcessing = "1";

        public MainWindow()
        {
            InitializeComponent();

            SelectPathTextBox.Text = _path;
            CheckIntervalTextBox.Text = _checkInterval;
            MaximumConcurrentProcessingTextBox.Text = _maximumConcurrentProcessing;

            IpcProvider.InitializeServer();
            
            IpcProvider.TriggerReceivedMessage += IpcProviderTriggerReceivedMessage;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
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
                IpcProvider.WriteMessageToClient(JsonSerializer.Serialize(
                    new IpcConfigurationMessage
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

        private void IpcProviderTriggerReceivedMessage(object? sender, IpcResultMessage? message)
        {
            if (message != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResultTextBox.Text += $"Message Received from : {message.Result}";
                }));
            }
        }
    }
}