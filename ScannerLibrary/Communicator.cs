// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Cognex.DataMan.SDK;
using Cognex.DataMan.SDK.Discovery;
using Cognex.DataMan.SDK.Utils;

//using PartTracking.ContainerModel;

namespace CognexPlayer
{
    /// <summary>
    /// Class for handling Barcode reader
    /// </summary>
    public class Communicator : IDisposable
    {
        #region Fields

        /// <summary>
        /// List of bar code reader types
        /// </summary>
        public enum ScannerType 
        {
            /// <summary>
            /// Handheld barcode reader
            /// </summary>
            Handheld,

            /// <summary>
            /// Fixed barcode reader
            /// </summary>
            Fixed 
        }

        private EthSystemConnector ethernetConnection;
        private DataManSystem dataManSystem;
        private EthSystemDiscoverer ethSystemDiscoverer;
        private ScannerType scannerType;
        private string previousBarCode = "PREVIOUS_BARCODE";
        private bool _ScannerOn;
        private System.Timers.Timer _previousBarCodeTimeout;
        private double _previousBarCodeTimeoutInterval = 4000;
        private string _workstationName;

        public delegate void AddResponse(string readString);
        
        public delegate bool Discovered(string ip);

        private event AddResponse AddResponseEvent;

        private event Discovered DiscoveredEvent;

        private Action<Exception> _onError;

        #endregion

        #region Constructor / Destructor / Dispose

        /// <summary>
        /// Initializes a new instance of the <see cref="Communicator" /> class
        /// </summary>
        /// <param name="resp">Barcode Scan Response callback</param>
        /// <param name="disc">Scanner Discovered callback</param>
        /// <param name="type">Barocde Reader Type</param>
        /// <param name="log">Global Logger</param>
        public Communicator(AddResponse resp, Discovered disc, ScannerType type, string workstationName, Action<Exception> onError)
        {
            _workstationName = workstationName;

            ethSystemDiscoverer = new EthSystemDiscoverer();
            AddResponseEvent += new AddResponse(resp);
            DiscoveredEvent += new Discovered(disc);
            scannerType = type;
            _ScannerOn = false;
            _previousBarCodeTimeout = new System.Timers.Timer();
            _previousBarCodeTimeout.Interval = _previousBarCodeTimeoutInterval;
            _previousBarCodeTimeout.AutoReset = false;

            _previousBarCodeTimeout.Elapsed += new System.Timers.ElapsedEventHandler(PreviousBarCodeTimeoutHandler);
            _previousBarCodeTimeout.Enabled = false;

            _onError = onError;
        }

        ~Communicator()
        {
            Disconnect();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Disconnect();
        }

        #endregion

        #region Methods

        public void QuickTurnOff()
        {
            ScannerOn = false;
        }

        /// <summary>
        /// Start barcoder discovering command
        /// </summary>
        public void Discover()
        {
            ethSystemDiscoverer.SystemDiscovered += new EthSystemDiscoverer.SystemDiscoveredHandler(OnEthSystemDiscovered);
            ethSystemDiscoverer.Discover();
        }

        /// <summary>
        /// Stop barcoder discovering command
        /// </summary>
        public void StopDiscovering()
        {
            if (ethSystemDiscoverer != null)
            {
                ethSystemDiscoverer.SystemDiscovered -= new EthSystemDiscoverer.SystemDiscoveredHandler(OnEthSystemDiscovered);
            }
        }

        /// <summary>
        /// Connect to barcoder command
        /// </summary>
        /// <param name="ipAddress">IP Address of the barcoder</param>
        /// <returns>Return true if connected, false otherwise</returns>
        public bool Connect(System.Net.IPAddress ipAddress)
        {
            ethernetConnection = new EthSystemConnector(ipAddress);

            ethernetConnection.UserName = "admin";
            ethernetConnection.Password = string.Empty;

            dataManSystem = new DataManSystem(ethernetConnection);
            dataManSystem.DefaultTimeout = 10000;

            dataManSystem.ReadStringArrived += new ReadStringArrivedHandler(OnReadStringArrived);

            ResultTypes resultType = ResultTypes.ReadString;

            try
            {
                dataManSystem.Connect();
                dataManSystem.SetResultTypes(resultType);
                TurnOff();
            }
            catch
            {
                try
                {
                    dataManSystem.Connect();
                    dataManSystem.SetResultTypes(resultType);
                    TurnOff();
                }
                catch (Exception e)
                {
                    _onError(new Exception(string.Format("Unable to connect to barcode reader. Message {0}, Type {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
                }
            }

            return true;
        }

        /// <summary>
        /// Disconnect from barcoder command
        /// </summary>
        public void Disconnect()
        {
            if (dataManSystem != null && dataManSystem.State == ConnectionState.Connected)
            {
                try
                {
                    dataManSystem.Disconnect();
                }
                catch (Cognex.DataMan.SDK.DataManException e)
                {
                    _onError(new Exception(string.Format("Unable to disconnect. Message: {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
                }
            }
        }

        /// <summary>
        /// Sends a command to a barcoder
        /// </summary>
        /// <param name="command">Command to be sent to a barcode reader</param>
        /// <returns>Response for command from the barcode reader, "Not sent" if the device is disconnected</returns>
        public string SendCommand(string command)
        {
            string output = "Not sent";

            if (dataManSystem != null && dataManSystem.State == ConnectionState.Connected)
            {
                try
                {
                    DmccResponse response = dataManSystem.SendCommand(command);
                    output = response.PayLoad;
                }
                catch (Cognex.DataMan.SDK.DataManException e)
                {
                    _onError(new Exception(string.Format("Unable to send a command to barcode reader. Message {0}, Type {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
                }
                catch (Exception e)
                {
                    try
                    {
                        DmccResponse response = dataManSystem.SendCommand(command);
                        output = response.PayLoad;
                    }
                    catch
                    {
                        _onError(new Exception(string.Format("This is a system or application exception in Communicator while trying to send a command to barcode reader. Message {0}, Type {1}, Command: {2}, Name: {2}", e.Message, e.GetType(), command, _workstationName), e));                    
                    }
                }
            }

            return output;
        }

        public void UploadConfigurationFile(string pathToFile)
        {
            if (dataManSystem != null && dataManSystem.State == ConnectionState.Connected)
            {
                try
                {
                    dataManSystem.SetConfig(pathToFile);
                }
                catch (Cognex.DataMan.SDK.DataManException e)
                {
                    _onError(new Exception(string.Format("Unable to set configuration file. Message {0}, Type {1}, Name: {2}", e.Message, e.GetType(), _workstationName), e));
                }
            }
        }

        /// <summary>
        /// Checks for a valid command
        /// </summary>
        /// <param name="cmd">Command to check validity</param>
        /// <returns>True if command is valid, False otherwise</returns>
        public bool IsCommandValid(string cmd)
        {
            string command = cmd.ToUpper();

            if (command == "GET DEVICE.TYPE" ||
                command == "TRIGGER ON" ||
                command == "TRIGGER OFF" ||
                command == "GET DEVICE.SERIAL-NUMBER" ||
                command == "GET RESULT")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if barcode reader is connected
        /// </summary>
        /// <returns>Returns True if barcode reader is connected, False otherwise</returns>
        public bool IsConnected()
        {
            bool connected = dataManSystem.State == ConnectionState.Connected;
            return connected;
        }

        /// <summary>
        /// Checks if barcode reader is connecting
        /// </summary>
        /// <returns>Returns True if barcode reader is connecting, False otherwise</returns>
        public bool IsConnecting()
        {
            bool connecting = dataManSystem.State == ConnectionState.Connecting;
            return connecting;
        }

        /// <summary>
        /// Checks if barcode reader is disconnected
        /// </summary>
        /// <returns>True if disconnected, False otherwise</returns>
        public bool IsDisconnected()
        {
            bool disconnected = dataManSystem.State == ConnectionState.Disconnected;
            return disconnected;
        }

        /// <summary>
        /// Checks if barcode reader is disconnecting
        /// </summary>
        /// <returns>True if disconnecting, False otherwise</returns>
        public bool IsDisconnecting()
        {
            bool disconnecting = dataManSystem.State == ConnectionState.Disconnecting;
            return disconnecting;
        }

        /// <summary>
        /// Turns scanner off
        /// </summary>
        public void TurnOff()
        {
            ScannerOn = false;

            if (TypeOfScanner == ScannerType.Handheld)
            {
                SendCommand("SET BEEP.GOOD 0 1");
                SendCommand("SET LIGHT.AIMER 0");
            }
            else
            {
                SendCommand("SET TRIGGER.TYPE 0");
                SendCommand("SET LIGHT.AIMER 0");
                SendCommand("SET BEEP.GOOD 0 0");
            }
        }

        /// <summary>
        /// Turn scanner on
        /// </summary>
        public void TurnOn()
        {
            ScannerOn = true;

            if (TypeOfScanner == ScannerType.Handheld)
            {
                SendCommand("SET BEEP.GOOD 1 1");
                SendCommand("SET LIGHT.AIMER 3");
                SendCommand("SET TRIGGER.TYPE 2");
            }
            else
            {
                SendCommand("SET BEEP.GOOD 3 2");
                SendCommand("SET LIGHT.AIMER 1");
                SendCommand("SET TRIGGER.TYPE 1");
                //previousBarCode = "PREVIOUS_BARCODE";
            }
        }

        /// <summary>
        /// Get currectly assinged IP address
        /// </summary>
        /// <returns>IP address string</returns>
        public string GetIpAddress()
        {
            string ip = "Undefined IP Address";

            if (ethernetConnection != null)
            {
                ip = ethernetConnection.Address.ToString();
            }

            return ip;
        }

        /// <summary>
        /// Subscribe to DataMan StringSubscribe event
        /// PLEASE DO NOT USE UNLESS ABSOLUTELY NEEDED
        /// </summary>
        public void SubscribeReadStringArrived()
        {
            dataManSystem.ReadStringArrived -= new ReadStringArrivedHandler(OnReadStringArrived);
            dataManSystem.ReadStringArrived += new ReadStringArrivedHandler(OnReadStringArrived);
        }

        #endregion

        #region Functions

        private void OnReadStringArrived(object sender, ReadStringArrivedEventArgs args)
        {
			string input = args.ReadString.Replace("\0", String.Empty);

            if (scannerType != ScannerType.Fixed || input != previousBarCode)
            {
                if (ScannerOn)
                {
                    dataManSystem.ReadStringArrived -= new ReadStringArrivedHandler(OnReadStringArrived);

                    if (scannerType == ScannerType.Fixed)
                    {
                        _previousBarCodeTimeout.Enabled = true;
                        _previousBarCodeTimeout.Stop();
                        _previousBarCodeTimeout.Start();
                    }

					previousBarCode = input;
                    AddResponseEvent(input);
                }
            }
        }

        private void OnEthSystemDiscovered(EthSystemDiscoverer.SystemInfo systemInfo)
        {
            string d = systemInfo.IPAddress.ToString();
            DiscoveredEvent(d);
        }

        /// <summary>
        /// Handler for previous part scan timeout for fixed barcode reader
        /// </summary>
        /// <param name="source">timer source</param>
        /// <param name="e">timer arguments</param>
        private void PreviousBarCodeTimeoutHandler(object source, System.Timers.ElapsedEventArgs e)
        {
            _previousBarCodeTimeout.Enabled = false;
            previousBarCode = "PREVIOUS_BARCODE";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a type of barcode reader: Handheld or Fixed
        /// </summary>
        public ScannerType TypeOfScanner
        {
            get
            {
                return scannerType;
            }

            set
            {
                if (value != scannerType)
                {
                    scannerType = value;
                }
            }
        }

        public bool ScannerOn
        {
            get
            {
                return _ScannerOn;
            }

            private set
            {
                if (value != _ScannerOn)
                {
                     _ScannerOn = value;
                }
            }
        }

        #endregion
    }
}
