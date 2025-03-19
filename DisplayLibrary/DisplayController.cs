// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using PartTracking.ContainerModel;

using Modbus.Data;
using Modbus.Device;
using Modbus.Utility;

namespace CognexPlayer
{
    #region Display Enums

    #region Display Backgrounds

    /// <summary>
    /// List of Dipslay Backgrounds
    /// </summary>
    public enum DisplayControllerPage 
    {
        /// <summary>
        /// Green Background Page
        /// </summary>
        DC_PASS = 0,

        /// <summary>
        /// Red Background Page
        /// </summary>
        DC_FAIL = 1,

        /// <summary>
        /// White Background Page
        /// </summary>
        DC_MESSAGE = 2,

        /// <summary>
        /// Green OK
        /// </summary>
        DC_PASS_OK = 3,

        /// <summary>
        /// Red NG
        /// </summary>
        DC_FAIL_NG = 4,

        /// <summary>
        /// Blue
        /// </summary>
        DC_BLUE = 5
    }

    #endregion

    #region Dispay Windows

    /// <summary>
    /// List of display windows
    /// </summary>
    public enum DisplayWindow 
    {
        /// <summary>
        /// Window without buttons
        /// </summary>
        DC_NO_BUTTON = 0,

        /// <summary>
        /// Window with a single button
        /// </summary>
        DC_1_BUTTON = 1,

        /// <summary>
        /// Window with 2 buttons
        /// </summary>
        DC_2_BUTTON = 2,

        /// <summary>
        /// Window with 3 buttons
        /// </summary>
        DC_3_BUTTON = 3
    }

    #endregion

    #region Display Buttons
 
    /// <summary>
    /// List of buttons
    /// </summary>
    public enum DipslayButtons 
    { 
        /// <summary>
        /// Button 1
        /// </summary>
        DC_BUTTON_1 = 0, 

        /// <summary>
        /// Button 2
        /// </summary>
        DC_BUTTON_2 = 1, 

        /// <summary>
        /// Button 3
        /// </summary>
        DC_BUTTON_3 = 2
    }

    #endregion

    #endregion

    /// <summary>
    /// Class for handling Display using Modbus TCP protocol
    /// </summary>
    public class DisplayController : IDisposable
    {
        #region Fields

        private const int DisplayCommunicationPort = 8000;
        private const int DisplayAddress = 10010;
        private const ushort Button1Address = 10;
        private const ushort Button2Address = 11;
        private const ushort Button3Address = 12;
        private const double ButtonAddressPullInterval = 1000;
        private const int ButtonLabelLength = 10;
        private const int Display_Page_DC_3_BUTTON_Address = 3;
        private const int Display_Page_DC_1_BUTTON_Address = 1;
        private const int Display_Page_DC_2_BUTTON_Address = 2;
        private const int Display_Page_DC_No_BUTTON_Translate_Address = 4;
        private const int Display_Page_DC_1_BUTTON_Translate_Address = 5;
        private const int Display_Page_DC_2_BUTTON_Translate_Address = 6;
        private const int Display_Page_DC_3_BUTTON_Translate_Address = 7;
        
        private readonly ushort DisplayMessageAddress = 10018;
        private readonly ushort DisplayMessageAddress2 = 10100;
        private readonly ushort DisplayMessageAddress3 = 10150;
        private readonly ushort Button1LabelAddress = 10050;
        private readonly ushort Button2LabelAddress = 10060;
        private readonly ushort Button3LabelAddress = 10070;
        
        private readonly int DisplayMessageLength = 32;

        private readonly bool translateToC;
 
        private Modbus.Device.ModbusIpMaster master = null;
        private TcpClient client = null;
        
        private DisplayPage mainPage;
        private DisplayButton button1;
        private DisplayButton button2;
        private DisplayButton button3;
        private DisplayMessage[] message;

        private DisplayMessage buttonLabelMessage1;
        private DisplayMessage buttonLabelMessage2;
        private DisplayMessage buttonLabelMessage3;

        private DisplayWindow currentWindow;

        public delegate void ButtonResponse(string commandText, string displayText);

        private event ButtonResponse ButtonResponseEvent;

        private Action<Exception> _onError;

        private string _workstationName = "Workstation Undefined";

        #endregion

        #region Constructor / Destructor / Dispose

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayController" /> class
        /// </summary>
        /// <param name="buttonresponse">Callback for a buttons response event</param>
        /// <param name="unicodeMessageEncoding">Flag to indicate that the messages will have unicode encoding</param>
        /// <param name="translate">Flag indicates if the message should be translated</param>
        /// <param name="log">Global logger</param>
        public DisplayController(ButtonResponse buttonresponse, bool unicodeMessageEncoding, bool translate, string workstationName, Action<Exception> onError)
        {
            _workstationName = workstationName;
            _onError = onError;
            try
            {
                translateToC = translate;

                if (unicodeMessageEncoding || translate)
                {
                    DisplayMessageAddress = 10200;
                    DisplayMessageAddress2 = 10250;
                    DisplayMessageAddress3 = 10300;

                    Button1LabelAddress = 10050;
                    Button2LabelAddress = 10080;
                    Button3LabelAddress = 10110;

                    if (translate)
                    {
                        DisplayMessageLength = 14;
                    }
                }
                else
                {
                    DisplayMessageAddress = 10018;
                    DisplayMessageAddress2 = 10100;
                    DisplayMessageAddress3 = 10150;

                    Button1LabelAddress = 10050;
                    Button2LabelAddress = 10060;
                    Button3LabelAddress = 10070;
                }

                mainPage = new DisplayPage(DisplayAddress, onError);
                button1 = new DisplayButton(Button1Address, ButtonAddressPullInterval, ButtonEvent1);
                button2 = new DisplayButton(Button2Address, ButtonAddressPullInterval, ButtonEvent2);
                button3 = new DisplayButton(Button3Address, ButtonAddressPullInterval, ButtonEvent3);
                message = new DisplayMessage[3]
                                            {
                                            new DisplayMessage(DisplayMessageAddress, DisplayMessageLength, unicodeMessageEncoding, onError),
                                            new DisplayMessage(DisplayMessageAddress2, DisplayMessageLength, unicodeMessageEncoding, onError),
                                            new DisplayMessage(DisplayMessageAddress3, DisplayMessageLength, unicodeMessageEncoding, onError)
                                            };
                buttonLabelMessage1 = new DisplayMessage(Button1LabelAddress, ButtonLabelLength, unicodeMessageEncoding, onError);
                buttonLabelMessage2 = new DisplayMessage(Button2LabelAddress, ButtonLabelLength, unicodeMessageEncoding, onError);
                buttonLabelMessage3 = new DisplayMessage(Button3LabelAddress, ButtonLabelLength, unicodeMessageEncoding, onError);
                ButtonResponseEvent += new ButtonResponse(buttonresponse);
                currentWindow = DisplayWindow.DC_3_BUTTON;
            }
            catch (Exception e)
            {
                _onError(e);
            }
        }

        ~DisplayController()
        {
            Disconnect();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Disconnect();
        }

        #endregion

        #region Functions

        /// <summary>
        /// Button 1 pull timer event handler
        /// </summary>
        /// <param name="source">timer source</param>
        /// <param name="e">timer arguments</param>
        private void ButtonEvent1(object source, System.Timers.ElapsedEventArgs e)
        {
            if (master != null && client.Connected)
            {
                try
                {
                    button1.AddressPullTimer.Stop();

                    bool[] inputs = master.ReadCoils(button1.Address, 1);

                    if (inputs[0])
                    {
                        if (!string.IsNullOrEmpty(button1.CommandText))
                        {
                            ButtonResponseEvent(button1.CommandText, button1.DisplayText);
                        }

                        master.WriteSingleCoil(button1.Address, false);
                    }

                    button1.AddressPullTimer.Start();
                }
                catch (Exception ex)
                {
                    _onError(new Exception(string.Format("ButtonEvent1. Message: {0}, Type: {1}", ex.Message, ex.GetType()), ex));
                }
            }
        }

        /// <summary>
        /// Button 2 pull timer event handler
        /// </summary>
        /// <param name="source">Source of the event raiser</param>
        /// <param name="e">Passed arguments</param>
        private void ButtonEvent2(object source, System.Timers.ElapsedEventArgs e)
        {
            if (master != null && client.Connected)
            {
                try
                {
                    button2.AddressPullTimer.Stop();

                    bool[] inputs = master.ReadCoils(button2.Address, 1);

                    if (inputs[0])
                    {
                        if (!string.IsNullOrEmpty(button2.CommandText))
                        {
                            ButtonResponseEvent(button2.CommandText, button2.DisplayText);
                        }

                        master.WriteSingleCoil(button2.Address, false);
                    }

                    button2.AddressPullTimer.Start();
                }
                catch (Exception ex)
                {
                    _onError(new Exception(string.Format("ButtonEvent2. Message: {0}, Type: {1}, Name: {2}", ex.Message, ex.GetType(), _workstationName), ex));
                }
            }
        }

        /// <summary>
        /// Button 3 pull timer event handler
        /// </summary>
        /// <param name="source">Source of the event raiser</param>
        /// <param name="e">Passed arguments</param>
        private void ButtonEvent3(object source, System.Timers.ElapsedEventArgs e)
        {
            if (master != null && client.Connected)
            {
                try
                {
                    button3.AddressPullTimer.Stop();

                    bool[] inputs = master.ReadCoils(button3.Address, 1);

                    if (inputs[0])
                    {
                        if (!string.IsNullOrEmpty(button3.CommandText))
                        {
                            ButtonResponseEvent(button3.CommandText, button3.DisplayText);
                        }

                        master.WriteSingleCoil(button3.Address, false);
                    }

                    button3.AddressPullTimer.Start();
                }
                catch (Exception ex)
                {
                    _onError(new Exception(string.Format("ButtonEvent3. Message: {0}, Type: {1}, Name: {2}", ex.Message, ex.GetType(), _workstationName), ex));
                }
            }
        }

        #endregion

        #region Methods

        #region Get Ip Address

        public string GetIpAddress()
        {
            string ip = "IP is undefined";

            if (master != null && client != null && client.Connected)
            {
                ip = client.Client.RemoteEndPoint.ToString();
            }

            return ip;
        }

        #endregion

        #region Connect

        /// <summary>
        /// Connects display at specific ip address
        /// </summary>
        /// <param name="address">Ip address of the display</param>
        /// <returns>True if connected, False otherwise</returns>
        public bool Connect(IPAddress address)
        {
            bool status = false;

            try
            {
                client = new TcpClient();
                var asyncResult = client.BeginConnect(address, DisplayCommunicationPort, null, null);
                var success = asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                if (!success)
                {
                    status = false;
                    button1.Disable();
                    button2.Disable();
                    button3.Disable();
                }
                else 
                {
                    master = ModbusIpMaster.CreateIp(client);
                    status = true;
                    button1.Enable();
                    button2.Enable();
                    button3.Enable();
                    master.WriteSingleCoil(button1.Address, false);
                    master.WriteSingleCoil(button2.Address, false);
                    master.WriteSingleCoil(button3.Address, false);
                    message[0].Reset(master, _workstationName);
                    message[1].Reset(master, _workstationName);
                    message[2].Reset(master, _workstationName);
                    master.WriteSingleCoil(Display_Page_DC_No_BUTTON_Translate_Address, false);
                    master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Translate_Address, false);
                    master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Translate_Address, false);
                    master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Translate_Address, false);
                    master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Address, false);
                    master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Address, false);
                    master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Address, false);
                }
            }
            catch (Exception e)
            {
                status = false;
                button1.Disable();
                button2.Disable();
                button3.Disable();

                _onError(new Exception(string.Format("Display Controller Connect(). Message: {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
            }

            return status;
        }

        #endregion

        #region Disable Inputs

        /// <summary>
        /// Disables user inputs from the display
        /// </summary>
        public void DisableInputs()
        {
            button1.Disable();
            button2.Disable();
            button3.Disable();
        }

        #endregion

        #region Enable Inputs

        /// <summary>
        /// Enables user inputs from the display
        /// </summary>
        public void EnableInputs()
        {
            button1.Enable();
            button2.Enable();
            button3.Enable();
        }

        #endregion

        #region Disconnect

        /// <summary>
        /// Disconects the display
        /// </summary>
        public void Disconnect()
        {
            if (client != null && client.Connected)
            {
                try
                {
                    client.Client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch (Exception e)
                {
                    _onError(new Exception(string.Format("Display Controller Disconnect(). Message: {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
                }
                finally 
                { 
                    button1.Disable();
                    button2.Disable();
                    button3.Disable();
                }
            }
        }

        #endregion

        #region Set Background

        /// <summary>
        /// Sets display background
        /// </summary>
        /// <param name="page">Display background</param>
        public void SetPage(DisplayControllerPage page)
        {
            if (master != null && client.Connected)
            {
                mainPage.SetBackground(page, master, _workstationName);
            }
        }

        #endregion

        #region Set Display Window

        /// <summary>
        /// Sets display window
        /// </summary>
        /// <param name="window">Window to be displayed</param>
        public void SetDisplayWindow(DisplayWindow window)
        {
            if (master != null && client.Connected)
            {
                currentWindow = window;

                try
                {
                    if (window == DisplayWindow.DC_NO_BUTTON)
                    {
                        if (translateToC)
                        {
                            master.WriteSingleCoil(Display_Page_DC_No_BUTTON_Translate_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Translate_Address, false);
                        }
                        else
                        {
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Address, false);
                        }

                        button1.Disable();
                        button2.Disable();
                        button3.Disable();
                    }
                    else if (window == DisplayWindow.DC_1_BUTTON)
                    {
                        if (translateToC)
                        {
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Translate_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_No_BUTTON_Translate_Address, false);
                        }
                        else
                        {
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Address, false);
                        }

                        button1.Enable();
                        button2.Disable();
                        button3.Disable();
                    }
                    else if (window == DisplayWindow.DC_2_BUTTON)
                    {
                        if (translateToC)
                        {
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Translate_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_No_BUTTON_Translate_Address, false);
                        }
                        else
                        {
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Address, false);
                        }

                        button1.Enable();
                        button2.Enable();
                        button3.Disable();
                    }
                    else if (window == DisplayWindow.DC_3_BUTTON)
                    {
                        if (translateToC)
                        {
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Translate_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Translate_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_No_BUTTON_Translate_Address, false);
                        }
                        else
                        {
                            master.WriteSingleCoil(Display_Page_DC_3_BUTTON_Address, true);
                            master.WriteSingleCoil(Display_Page_DC_1_BUTTON_Address, false);
                            master.WriteSingleCoil(Display_Page_DC_2_BUTTON_Address, false);
                        }

                        button1.Enable();
                        button2.Enable();
                        button3.Enable();
                    }
                }
                catch (Exception e)
                {
                    _onError(new Exception(string.Format("Display Controller SetDisplayWindow(). Message: {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
                }
            }
        }

        #endregion

        #region Clear Message

        /// <summary>
        /// Clears message on the display
        /// </summary>
        public void ClearMessage()
        {
            try
            {
                foreach (var m in message)
                {
                    m.Clear(master, _workstationName);
                }
            }
            catch (DisplayException e)
            {
                _onError(new Exception(string.Format("Unable to clear message. {0}, Name: {1}", e.Message, _workstationName), e));
            }
        }

        #endregion

        #region Set Display Message

        /// <summary>
        /// Sets 32 characters message on display
        /// </summary>
        /// <param name="msg">message to be displayed</param>
        public void SetMessage(string msg)
        {
            if (master == null || !client.Connected)
                throw new Exception("Workstation not ready");

            if (msg.Length == 2)
            {
                message[0].Clear(master, _workstationName);
                message[2].Clear(master, _workstationName);

                if (translateToC)
                {
                    msg = LanguageTranslator.Instance.Translate(msg);
                }

                message[1].Set(msg, master, _workstationName);
            }
            else
            {
                if (translateToC)
                {
                    msg = LanguageTranslator.Instance.Translate(msg);
                    message[0].Set(msg, master, _workstationName);
                    message[1].Clear(master, _workstationName);
                }
                else
                {
                    var lines = SplitMessage(msg, 3, DisplayMessageLength);

                    for (int i = 0; i < 3; i++)
                    {
                        if (i< lines.Length)
                            message[i].Set(lines[i], master, _workstationName);
                        else
                            message[i].Clear(master, _workstationName);
                    }
                }
            }
        }

        public static string[] SplitMessage(string msg, int lines, int charPerLine)
        {
            var result = msg.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).SelectMany(l => SplitOnLenth(l, charPerLine)).ToArray();

            if (result.Length > lines)
                throw new InvalidOperationException(string.Format("Could not display text '{0}'.  It requires {1} lines.", msg, result.Length));

            return result;
        }

        public static IEnumerable<string> SplitOnLenth(string line, int charPerLine)
        {
            if (line.Length <= charPerLine)
                yield return line;
            else
            {
                var spaceIndex = line.Substring(0, charPerLine + 1).LastIndexOf(' ');

                if (spaceIndex == -1)
                    throw new InvalidOperationException("Word too long");

                if (spaceIndex != 0)
                    yield return line.Substring(0, spaceIndex);

                foreach (var s in SplitOnLenth(line.Substring(spaceIndex + 1), charPerLine))
                    yield return s;
            }
        }

        #endregion

        #region Display Connection Test

        /// <summary>
        /// Checks if display is connected
        /// </summary>
        /// <returns>True if connected, False otherwise</returns>
        public bool IsConnected()
        {
            bool statusOK = master != null && client.Connected;
            return statusOK;
        }

        #endregion

        #region Set Button Label

        /// <summary>
        /// Sets button's label
        /// </summary>
        /// <param name="buttonNumber">Button identifier</param>
        /// <param name="commandText">Button's command</param>
        /// <param name="displayText">Label text</param>
        public void SetButtonLabel(DipslayButtons buttonNumber, string commandText, string displayText)
        {
            if (translateToC)
            {
                displayText = LanguageTranslator.Instance.Translate(displayText);
            }

            if (buttonNumber == DipslayButtons.DC_BUTTON_1)
            {
                buttonLabelMessage1.Set(displayText, master, _workstationName);
                button1.CommandText = commandText;
                button1.DisplayText = displayText;
            }
            else if (buttonNumber == DipslayButtons.DC_BUTTON_2)
            {
                buttonLabelMessage2.Set(displayText, master, _workstationName);
                button2.CommandText = commandText;
                button2.DisplayText = displayText;
            }
            else if (buttonNumber == DipslayButtons.DC_BUTTON_3)
            {
                buttonLabelMessage3.Set(displayText, master, _workstationName);
                button3.CommandText = commandText;
                button3.DisplayText = displayText;
            }
        }

        #endregion

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets currently set display window
        /// </summary>
        public DisplayWindow CurrentWindow
        {
            get
            {
                return currentWindow;
            }
        }

        #endregion
    }
}
