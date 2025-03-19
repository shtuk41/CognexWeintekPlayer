// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PartTracking.ContainerModel;

namespace CognexPlayer
{
    /// <summary>
    /// Class to handle Display Message
    /// </summary>
    internal class DisplayMessage
    {
        #region Fields

        private ushort _MessageAddress;
        private int _MessageLength;
        private Action<Exception> _onError;
        private bool _UnicodeEncoding;
        private string _MessagePreviouslySet;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayMessage"/> class
        /// </summary>
        /// <param name="messageAddress">Modbus address for the message</param>
        /// <param name="length">Length of the message defined for the message</param>
        /// <param name="unicodeEncoding">Unicode parameter</param>
        /// <param name="log">Reference to logger</param>
        public DisplayMessage(ushort messageAddress, int length, bool unicodeEncoding, Action<Exception> onError)
        {
            _MessageAddress = messageAddress;
            _MessageLength = length;
            _onError = onError;
            _UnicodeEncoding = unicodeEncoding;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets message address
        /// </summary>
        public ushort MessageAddress
        {
            get
            {
                return _MessageAddress;
            }
        }

        /// <summary>
        /// Gets defined message length
        /// </summary>
        public int MessageLength
        {
            get
            {
                return _MessageLength;
            }
        }

        #endregion

        #region Methods

        #region Clear Message
 
        /// <summary>
        /// Clears message
        /// </summary>
        /// <param name="master">Modbus device</param>
        /// <param name="ipAddress">Master Ip Address</param>
        public void Clear(Modbus.Device.ModbusIpMaster master, string workstationName)
        {
            string message = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(_MessagePreviouslySet))
                {
                    _MessagePreviouslySet = message;
                    Set(message, master, workstationName);
                }
            }
            catch (Exception e)
            {
                _onError(new Exception(string.Format("Unable to Clear message.  Message: {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), workstationName), e));
            }
        }

        #endregion

        #region Set Message
 
        /// <summary>
        /// Sets a string
        /// </summary>
        /// <param name="messageIn">string to set a message</param>
        /// <param name="master">Modbus device</param>
        public void Set(string messageIn, Modbus.Device.ModbusIpMaster master, string workstationName)
        {
            _MessagePreviouslySet = messageIn;

            if (_UnicodeEncoding)
            {
                SetUnicode(messageIn, master, workstationName);
            }
            else
            {
                SetAscii(messageIn, master, workstationName);
            }
        }

        /// <summary>
        /// Sets Ascii message
        /// </summary>
        /// <param name="messageIn">Message to be set on display</param>
        /// <param name="master">Modbus device</param>
        private void SetAscii(string messageIn, Modbus.Device.ModbusIpMaster master, string ipAddress)
        {
            string message;

            if (messageIn.Length > _MessageLength)
            {
                message = messageIn.Substring(0, _MessageLength);
            }
            else
            {
                message = messageIn;
            }

            byte[] array = Encoding.ASCII.GetBytes(message);

            ////THis is an example for Unicode, don't delete
            ////byte[] array = new byte[] { 0x76, 0x4e, 0x12, 0x4e };
            ////byte[] array = new byte[] { 0x31, 0x59, 0x25, 0x8d};

            ushort startAdress = MessageAddress;

            for (int ii = 0; ii < _MessageLength; ii += 2)
            {
                byte[] barray = new byte[2];

                if (array.Length > ii + 1)
                {
                    barray[0] = array[ii];
                    barray[1] = array[ii + 1];
                }
                else if (array.Length > ii)
                {
                    barray[0] = array[ii];
                    barray[1] = 32;
                }
                else
                {
                    barray[0] = 32;
                    barray[1] = 32;
                }

                ushort toSendAscii = BitConverter.ToUInt16(barray, 0);
                
                try
                {
                    master.WriteMultipleRegisters(startAdress, new ushort[] { toSendAscii });
                }
                catch (Exception e)
                {
                    _onError(new Exception(string.Format("Set Message Operation Error. Message {0}, Type: {1}, Ip: {2}", e.Message, e.GetType(), ipAddress), e));
                }

                startAdress += 1;
            }
        }
        
        #endregion

        #region Set Message

        /// <summary>
        /// Sets message
        /// </summary>
        /// <param name="messageIn">Message to be set on display</param>
        /// <param name="master">Modbus device</param>
        private void SetUnicode(string messageIn, Modbus.Device.ModbusIpMaster master, string workstationName)
        {
            string message;

            if (messageIn.Length > _MessageLength)
            {
                message = messageIn.Substring(0, _MessageLength);
            }
            else
            {
                message = messageIn;
            }

            byte[] array = Encoding.Unicode.GetBytes(message);

            ////THis is an example for Unicode, don't delete
            ////byte[] array = new byte[] { 0x76, 0x4e, 0x12, 0x4e };
            ////byte[] array = new byte[] { 0x31, 0x59, 0x25, 0x8d };

            ushort startAdress = MessageAddress;

            for (int ii = 0; ii < _MessageLength * 2; ii += 2)
            {
                byte[] barray = new byte[2];

                if (array.Length > ii + 1)
                {
                    barray[0] = array[ii];
                    barray[1] = array[ii + 1];
                }
                else if (array.Length > ii)
                {
                    barray[0] = array[ii];
                    barray[1] = 32;
                }
                else
                {
                    barray[0] = 32;
                    barray[1] = 0;
                }

                ushort toSendAscii = BitConverter.ToUInt16(barray, 0);

                try
                {
                    master.WriteMultipleRegisters(startAdress, new ushort[] { toSendAscii });
                }
                catch (Exception e)
                {
                    _onError(new Exception(string.Format("Set Message Operation Error. Message {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), workstationName), e));
                }

                startAdress += 1;
            }
        }

        #endregion

        #region Reset Message

        /// <summary>
        /// Clears message
        /// </summary>
        /// <param name="master">Modbus device</param>
        public void Reset(Modbus.Device.ModbusIpMaster master, string workstationName)
        {
            string message = string.Empty;

            try
            {
                Set(message, master, workstationName);
            }
            catch (Exception e)
            {
                _onError(new Exception(string.Format("Unable to Reset message.  Message: {0}, Type: {1}, Name: {2}", e.Message, e.GetType(), workstationName), e));
            }
        }

        #endregion

        #endregion
    }
}
