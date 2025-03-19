// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using PartTracking.Hardware.Cognex.Display;
using CognexPlayer;


namespace CognexPlayer
{
    /// <summary>
    /// Model view for Cognex Player
    /// </summary>
    internal class CognexPlayerModelView : INotifyPropertyChanged
    {
        #region fields

        private Communicator communicator;
        private DisplayController displayController;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CognexPlayerModelView" /> class
        /// </summary>
        public CognexPlayerModelView()
        {
            communicator = new Communicator(AddResponse, Discovered, Communicator.ScannerType.Fixed, "Test Scanner", OnException);
            displayController = new DisplayController(ButtonResponse, true, false, "Test Workstation", 20, OnException);
        }

        #endregion

        #region Methods and Callbacks

        #region Connect

        /// <summary>
        /// Logic for connecting to barcode reader button
        /// </summary>
        public void Connect()
        {
            if (ConnectContent == "Connect")
            {
                System.Net.IPAddress ipAdress;

                if (System.Net.IPAddress.TryParse(DeviceIpAdress, out ipAdress))
                {
                    try
                    {
                        if (communicator.Connect(ipAdress))
                        {
                            ConnectContent = "Disconnect";
                            ConnectForeground = System.Windows.Media.Brushes.Green;
                        }
                    }
                    catch (BarcodeReaderException e)
                    {
                        Response += string.Format("Unable to connect.  Message: {0}, Error Type: {1}", e.Message, e.GetType());
                        ConnectContent = "Connect";
                        ConnectForeground = System.Windows.Media.Brushes.Black;
                    }
                }
            }
            else
            {
                try
                {
                    communicator.Disconnect();
                }
                catch (BarcodeReaderException e)
                {
                    Response += string.Format("Unable to disconnect the barcode reader. Message: {0}, Type: {1}", e.Message, e.GetType());
                    Response += "\n";
                }
                finally
                {
                    ConnectContent = "Connect";
                    ConnectForeground = System.Windows.Media.Brushes.Black;
                }
            }
        }

        #endregion

        #region Send Command

        /// <summary>
        /// Logic for Send button
        /// </summary>
        public void SendCommand()
        {
            if (ConnectContent == "Disconnect")
            {
                try
                {
                    string resp = communicator.SendCommand(DeviceCommand);
                    if (!string.IsNullOrEmpty(resp))
                    {
                        ////Response += communicator.SendCommand(DeviceCommand);
                        Response += resp;
                        Response += "\n";
                    }
                }
                catch (BarcodeReaderException e)
                {
                    Response += e.Message;
                    Response += "\n";
                }
            }
        }

        #endregion

        #region Is Command Valid

        /// <summary>
        /// Checks if user entered command is valid
        /// </summary>
        /// <param name="command">Command for barcode reader</param>
        /// <returns>True if command is valid, False otherwise</returns>
        public bool IsCommandValid(string command)
        {
            bool valid = communicator.IsCommandValid(command);
            return valid;
        }

        #endregion

        #region Discover

        /// <summary>
        /// Logic for Discover button
        /// </summary>
        public void Discover()
        {
            communicator.Discover();
        }

        #endregion

        #region UploadScannerConfigurationFile

        public void UploadScannerConfigurationFile()
        {
            if (ConnectContent == "Disconnect")
            {
                try
                {
                    communicator.UploadConfigurationFile(ConfigFileLocation);
                }
                catch (Exception ex)
                {

                }
            }
        }

        #endregion

        #region Set Display Page

        /// <summary>
        /// Sets display Page: Pass - Green, Fail - Red, Message - White
        /// </summary>
        /// <param name="radioButtonContent">Radio button content</param>
        public void SetDisplayPage(string radioButtonContent)
        {
            try
            {
                if (radioButtonContent == "Pass")
                {
                    displayController.SetPage(DisplayControllerPage.DC_PASS);
                }
                else if (radioButtonContent == "Fail")
                {
                    displayController.SetPage(DisplayControllerPage.DC_FAIL);
                }
                else if (radioButtonContent == "Pass OK")
                {
                    displayController.SetPage(DisplayControllerPage.DC_PASS_OK);
                }
                else if (radioButtonContent == "Fail NG")
                {
                    displayController.SetPage(DisplayControllerPage.DC_FAIL_NG);
                }
                else if (radioButtonContent == "Blue")
                {
                    displayController.SetPage(DisplayControllerPage.DC_BLUE);
                }
                else if (radioButtonContent == "Read")
                {
                    displayController.SetPage(DisplayControllerPage.DC_READ);
                }
                else if (radioButtonContent == "ReadM")
                {
                    displayController.SetPage(DisplayControllerPage.DC_READ_MANDARIN);
                }
                else
                {
                    displayController.SetPage(DisplayControllerPage.DC_MESSAGE);
                }
            }
            catch (DisplayException e)
            {
                Response += e.Message;
                Response += "\n";
            }
        }

        #endregion

        #region Set Display Window based on Number of buttons

        /// <summary>
        /// Sets display window based on a number of buttons
        /// </summary>
        /// <param name="radioButtonContent">String identifying number of buttons that should be displayed</param>
        public void SetDisplayNumberOfButtons(string radioButtonContent)
        {
            try
            {
                if (radioButtonContent == "No Buttons")
                {
                    displayController.SetDisplayWindow(DisplayWindow.DC_NO_BUTTON);
                }
                else if (radioButtonContent == "1 Button")
                {
                    displayController.SetDisplayWindow(DisplayWindow.DC_1_BUTTON);
                }
                else if (radioButtonContent == "2 Buttons")
                {
                    displayController.SetDisplayWindow(DisplayWindow.DC_2_BUTTON);
                }
                else if (radioButtonContent == "3 Buttons")
                {
                    displayController.SetDisplayWindow(DisplayWindow.DC_3_BUTTON);
                }
            }
            catch (DisplayException e)
            {
                Response += e.Message;
                Response += "\n";
            }
        }

        #endregion

        #region Connect Display

        /// <summary>
        /// Logic for handling Connect to display button
        /// </summary>
        public void ConnectDisplay()
        {
            try
            {
                if (ConnectDisplayContent == "Connect" && displayController.Connect(System.Net.IPAddress.Parse(DisplayIPAddress)))
                {
                    ConnectDisplayContent = "Disconnect";
                    ConnectDisplayForeground = System.Windows.Media.Brushes.Green;
                    DisplayIPTextBoxEnabled = false;
                    displayController.SetButtonLabel(DipslayButtons.DC_BUTTON_1, "BTN1CMD", "BTN1TEXT");
                    displayController.SetButtonLabel(DipslayButtons.DC_BUTTON_2, "BTN2CMD", "BTN2TEXT");
                    displayController.SetButtonLabel(DipslayButtons.DC_BUTTON_3, "BTN3CMD", "BTN3TEXT");
                    displayController.SetDisplayWindow(DisplayWindow.DC_3_BUTTON);
                }
                else if (ConnectDisplayContent == "Disconnect")
                {
                    ConnectDisplayContent = "Connect";
                    displayController.Disconnect();
                    ConnectDisplayForeground = System.Windows.Media.Brushes.Black;
                    DisplayIPTextBoxEnabled = true;
                }
            }
            catch (DisplayException e)
            {
                Response += e.Message;
                Response += "\n";
            }
        }

        #endregion

        #region Set Message

        /// <summary>
        /// Logic for settings a user's message
        /// </summary>
        public void SetMessage()
        {
            try
            {
                displayController.SetMessage(DisplayMessageSet);
            }
            catch (DisplayException e)
            {
                Response += e.Message;
                Response += "\n";
            }
        }

        #endregion

        #region Set Scanner On/Off

        public void SetScannerOnOff(string status)
        {
            if (status == "On")
            {
                communicator.TurnOn();
            }
            else
            {
                communicator.TurnOff();
            }
        }

        #endregion

        #region AddResponse

        /// <summary>
        /// Callback for scanner's response
        /// </summary>
        /// <param name="response">Response from the barcode reader</param>
        public void AddResponse(string response)
        {
            Response += response;
            Response += "\n";
        }

        #endregion

        #region ButtonResponse

        /// <summary>
        /// Callback for Display button response
        /// </summary>
        /// <param name="commandtext">Button's command</param>
        /// <param name="displaytext">BUtton's display text</param>
        public void ButtonResponse(string commandtext, string displaytext)
        {
            Response += string.Format("Button Command: {0}, Button Text: {1}", commandtext, displaytext);
            Response += "\n";
        }

        #endregion

        #region Discovered

        /// <summary>
        /// Callback for barcode reader discovered event
        /// </summary>
        /// <param name="ip">Ip address of the barcode reader</param>
        /// <returns>True if discovered, False otherwise</returns>
        public bool Discovered(string ip)
        {
            if (!string.IsNullOrEmpty(ip))
            {
                DeviceIpAdress = ip;
                communicator.StopDiscovering();
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        private void OnException(Exception e)
        {
            throw e;
        }

        #endregion

        #region Properties

        #region Device Ip Adress

        /// <summary>
        /// Gets or sets Device Ip Address
        /// </summary>
        public string DeviceIpAdress
        {
            get
            {
                return _DeviceIpAdress;
            }

            set
            {
                if (value != DeviceIpAdress)
                {
                    _DeviceIpAdress = value;
                    OnPropertyChanged("DeviceIpAdress");
                }
            }
        }

        private string _DeviceIpAdress = string.Empty;

        #endregion

        #region Device Command

        /// <summary>
        /// Gets or sets Device Command
        /// </summary>
        public string DeviceCommand
        {
            get
            {
                return _DeviceCommand;
            }

            set
            {
                if (value != DeviceCommand)
                {
                    _DeviceCommand = value;
                    OnPropertyChanged("DeviceCommand");
                }
            }
        }

        private string _DeviceCommand = "GET DEVICE.TYPE";
        #endregion

        #region DeviceCommandBackground

        /// <summary>
        /// Gets or sets Device Command Background
        /// </summary>
        public System.Windows.Media.Brush DeviceCommandBackground
        {
            get
            {
                return _DeviceCommandBackground;
            }

            set
            {
                if (value != DeviceCommandBackground)
                {
                    _DeviceCommandBackground = value;
                    OnPropertyChanged("DeviceCommandBackground");
                }
            }
        }

        private System.Windows.Media.Brush _DeviceCommandBackground = System.Windows.Media.Brushes.White;
        #endregion

        #region Response

        /// <summary>
        /// Gets or sets Response
        /// </summary>
        public string Response
        {
            get
            {
                return _Response;
            }

            set
            {
                if (value != _Response)
                {
                    _Response = value;
                    OnPropertyChanged("Response");
                }
            }
        }

        private string _Response = string.Empty;

        #endregion

        #region ConnectContent

        /// <summary>
        /// Gets or sets Connect button content
        /// </summary>
        public string ConnectContent
        {
            get
            {
                return _ConnectContent;
            }

            set
            {
                if (value != ConnectContent)
                {
                    _ConnectContent = value;
                    OnPropertyChanged("ConnectContent");
                }
            }
        }

        private string _ConnectContent = "Connect";
        #endregion

        #region ConnectForeground

        /// <summary>
        /// Gets or sets Connect button foreground
        /// </summary>
        public System.Windows.Media.Brush ConnectForeground
        {
            get
            {
                return _ConnectForeground;
            }

            set
            {
                if (value != ConnectForeground)
                {
                    _ConnectForeground = value;
                    OnPropertyChanged("ConnectForeground");
                }
            }
        }

        private System.Windows.Media.Brush _ConnectForeground = System.Windows.Media.Brushes.Black;
        #endregion

        #region DisplayIPAddress

        /// <summary>
        /// Gets or sets Display Ip Address
        /// </summary>
        public string DisplayIPAddress
        {
            get
            {
                return _DisplayIPAddress;
            }

            set
            {
                if (value != DisplayIPAddress)
                {
                    _DisplayIPAddress = value;
                    OnPropertyChanged("DisplayIPAddress");
                }
            }
        }

        private string _DisplayIPAddress = "169.253.20.9";
        #endregion

        #region DisplayMessageSet

        /// <summary>
        /// Gets or sets Display Message
        /// </summary>
        public string DisplayMessageSet
        {
            get
            {
                return _DisplayMessageSet;
            }

            set
            {
                if (value != DisplayMessageSet)
                {
                    _DisplayMessageSet = value;
                    OnPropertyChanged("DisplayMessageSet");
                }
            }
        }

        private string _DisplayMessageSet = "32 character message";
        #endregion

        #region ConnectDisplay

        /// <summary>
        /// Gets or sets Connect Display button content
        /// </summary>
        public string ConnectDisplayContent
        {
            get
            {
                return _ConnectDisplayContent;
            }

            set
            {
                if (value != ConnectDisplayContent)
                {
                    _ConnectDisplayContent = value;
                    OnPropertyChanged("ConnectDisplayContent");
                }
            }
        }

        private string _ConnectDisplayContent = "Connect";
        #endregion

        #region ConnectDisplayForeground

        /// <summary>
        /// Gets or sets Connect Display button Foreground
        /// </summary>
        public System.Windows.Media.Brush ConnectDisplayForeground
        {
            get
            {
                return _ConnectDisplayForeground;
            }

            set
            {
                if (value != ConnectDisplayForeground)
                {
                    _ConnectDisplayForeground = value;
                    OnPropertyChanged("ConnectDisplayForeground");
                }
            }
        }

        private System.Windows.Media.Brush _ConnectDisplayForeground = System.Windows.Media.Brushes.Black;
        #endregion

        #region DisplayIPTextBoxEnabled

        /// <summary>
        /// Gets or sets a value indicating whether the Display Ip Text Box is enabled
        /// </summary>
        public bool DisplayIPTextBoxEnabled
        {
            get
            {
                return _DisplayIPTextBoxEnabled;
            }

            set
            {
                if (value != DisplayIPTextBoxEnabled)
                {
                    _DisplayIPTextBoxEnabled = value;
                    OnPropertyChanged("DisplayIPTextBoxEnabled");
                }
            }
        }

        private bool _DisplayIPTextBoxEnabled = true;

        #endregion

        #region ConfigFileLocation

        public string ConfigFileLocation
        {
            get
            {
                return _ConfigFileLocation;
            }

            set
            {
                if (value != _ConfigFileLocation)
                {
                    _ConfigFileLocation = value;
                }
            }
        }

        private string _ConfigFileLocation = string.Empty;

        #endregion

        #endregion
    }
}
