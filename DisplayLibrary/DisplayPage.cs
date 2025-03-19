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
    /// Class for handling Display backgrounds
    /// </summary>
    internal class DisplayPage
    {
        #region fields

        private ushort _BackgroundAddress;

        private DisplayControllerPage _Background;

        private Action<Exception> _onError;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPage" /> class.
        /// </summary>
        /// <param name="backgroundAdress">Modbus address of the background</param>
        /// <param name="log">Reference to logger</param>
        public DisplayPage(ushort backgroundAdress, Action<Exception> onError)
        {
            _BackgroundAddress = backgroundAdress;
            _onError = onError;
        }

        #endregion

        #region methods

        #region Set Background

        /// <summary>
        /// Sets display background
        /// </summary>
        /// <param name="background">Background page</param>
        /// <param name="master">Modbus device</param>
        public void SetBackground(DisplayControllerPage background, Modbus.Device.ModbusIpMaster master, string workstationName)
        {
            _Background = background;

            byte[] bytes = BitConverter.GetBytes((float)background);
            byte[] bytesMoved = new byte[2] { bytes[2], bytes[3] };
            ushort toSend = BitConverter.ToUInt16(bytesMoved, 0);

            try
            {
                master.WriteMultipleRegisters(BackgroundAddress, new ushort[] { toSend });
            }
            catch (Exception e)
            {
                _onError(new Exception(string.Format("Unable to set background. Message {0}, Type {1}, Name: {2}", e.Message, e.GetType(), workstationName), e));
            }
        }

        #endregion

        #endregion

        #region properties

        /// <summary>
        /// Gets Background Address
        /// </summary>
        public ushort BackgroundAddress
        {
            get
            {
                return _BackgroundAddress;
            }
        }

        #endregion
    }
}
