// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CognexPlayer
{
    public class DisplayException : ApplicationException
    {
        public DisplayException() : base("Check Display Connection and Operations")
        {
        }

        public DisplayException(string msg) : base(msg)
        {
        }
    }
}
