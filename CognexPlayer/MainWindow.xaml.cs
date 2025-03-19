// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.WindowsAPICodePack.Dialogs;

namespace CognexPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CognexPlayerModelView _ViewModel = new CognexPlayerModelView();
        private CommonOpenFileDialog _FolderBrowserDialog = new CommonOpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = _ViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            _ViewModel.Connect();
        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            _ViewModel.SendCommand();
        }

        private void TextChanged_DeviceIpAddress(object sender, RoutedEventArgs e)
        {
            System.Net.IPAddress address;

            if (System.Net.IPAddress.TryParse(textBoxDeviceIPAddress.Text, out address))
            {
                textBoxDeviceIPAddress.Background = System.Windows.Media.Brushes.White;
            }
            else
            {
                textBoxDeviceIPAddress.Background = System.Windows.Media.Brushes.Yellow;
            }
        }

        private void TextBoxCommand_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_ViewModel.IsCommandValid(textBoxCommand.Text))
            {
                textBoxCommand.Background = System.Windows.Media.Brushes.White;
            }
            else
            {
                textBoxCommand.Background = System.Windows.Media.Brushes.Yellow;
            }
        }

        private void ButtonDiscover_Click(object sender, RoutedEventArgs e)
        {
            _ViewModel.Discover();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;

            string content = button.Content.ToString();

            _ViewModel.SetDisplayPage(content);
        }

        private void ButtonConnectDisplay_Click(object sender, RoutedEventArgs e)
        {
            _ViewModel.ConnectDisplay();
        }

        private void ButtonSetDisplayMessage_Click(object sender, RoutedEventArgs e)
        {
            _ViewModel.SetMessage();
        }

        private void RadioButtonNoB_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;

            string content = button.Content.ToString();

            _ViewModel.SetDisplayNumberOfButtons(content);
        }

        private void RadioButtonScannerOnOff_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;

            string content = button.Content.ToString();

            _ViewModel.SetScannerOnOff(content);
        }

        private void TextChanged_DeviceIpAddress(object sender, TextChangedEventArgs e)
        {

        }

        public CommonOpenFileDialog FolderBrowserDialog
        {
            get
            {
                return _FolderBrowserDialog;
            }
        }

        private void buttonSelectConfig_Click(object sender, RoutedEventArgs e)
        {
            //FolderBrowserDialog.InitialDirectory = _ViewModel.RootDestinationDirectory;
            FolderBrowserDialog.IsFolderPicker = false;

            if (FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (File.Exists(FolderBrowserDialog.FileName))
                {
                    // Update the variable and textbox with the input volume directory
                    _ViewModel.ConfigFileLocation = FolderBrowserDialog.FileName;
                }
                else
                {
                    // The directory does NOT exist and the user must be notified
                    string msg = "Selected file doesn't exist";
                    MessageBox.Show(msg);
                }
            }

            _ViewModel.UploadScannerConfigurationFile();
        }
    }
}
