// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CognexPlayer
{
    /// <summary>
    /// Class that handles operations of the software button on a display
    /// </summary>
    internal class DisplayButton
    {
        #region Fields

        private ushort _Address;
        private double _AddressPullInterval;
        private string _CommandText;
        private string _DisplayText;
        private System.Timers.Timer _AddressPullTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayButton" /> class
        /// </summary>
        /// <param name="buttonAddress">Modbus address for the button</param>
        /// <param name="addressPullInterval">Interval for a timer used to pull a value of the button</param>
        /// <param name="buttonPressedHandler">Callback for button pressed</param>
        public DisplayButton(ushort buttonAddress, double addressPullInterval, System.Timers.ElapsedEventHandler buttonPressedHandler)
        {
            Address = buttonAddress;
            AddressPullInterval = addressPullInterval;

            AddressPullTimer = new System.Timers.Timer();

            if (buttonPressedHandler != null)
            {
                AddressPullTimer.Elapsed += new System.Timers.ElapsedEventHandler(buttonPressedHandler);
            }

            AddressPullTimer.Interval = addressPullInterval;

            AddressPullTimer.AutoReset = false;

            Disable();
        }

        #endregion

        #region methods

        /// <summary>
        /// Enables timer for pulling a button's state
        /// </summary>
        public void Enable()
        {
            AddressPullTimer.Enabled = true;
        }

        /// <summary>
        /// Disables timer for pulling a button's state
        /// </summary>
        public void Disable()
        {
            AddressPullTimer.Enabled = false;
        }

        /// <summary>
        /// CHecks if the timer is enabled
        /// </summary>
        /// <returns>True if value pull timer is enabled, False otherwise</returns>
        public bool IsEnabled()
        {
            return AddressPullTimer.Enabled;
        }
        #endregion

        #region Properties

        #region Address

        /// <summary>
        /// Gets or sets Modbus address of the button
        /// </summary>
        public ushort Address
        {
            get
            {
                return _Address;
            }

            set
            {
                if (value != _Address)
                {
                    _Address = value;
                }
            }
        }

        #endregion

        #region AddressPullInterval

        /// <summary>
        /// Gets or sets interval for a timer used to pull the value of the button state
        /// </summary>
        private double AddressPullInterval
        {
            get
            {
                return _AddressPullInterval;
            }

            set
            {
                if (value != AddressPullInterval)
                {
                    _AddressPullInterval = value;
                }
            }
        }

        #endregion

        #region Address Pull Timer

        /// <summary>
        /// Gets or sets a timer used to pull the value of the button
        /// </summary>
        public System.Timers.Timer AddressPullTimer
        {
            get
            {
                return _AddressPullTimer;
            }

            set
            {
                if (value != AddressPullTimer)
                {
                    _AddressPullTimer = value;
                }
            }
        }
        #endregion

        #region Command Text

        /// <summary>
        /// Gets or sets a command triggered by pushing a button
        /// </summary>
        public string CommandText
        {
            get
            {
                return _CommandText;
            }

            set
            {
                if (value != _CommandText)
                {
                    _CommandText = value;
                }
            }
        }

        #endregion

        #region Display Text

        /// <summary>
        /// Gets or sets a button label text
        /// </summary>
        public string DisplayText
        {
            get
            {
                return _DisplayText;
            }

            set
            {
                if (value != _DisplayText)
                {
                    _DisplayText = value;
                }
            }
        }
        #endregion

        #endregion
    }
}
