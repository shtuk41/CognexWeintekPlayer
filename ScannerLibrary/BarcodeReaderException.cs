// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CognexPlayer
{
    public class BarcodeReaderException : ApplicationException
    {
        public BarcodeReaderException() : base("Check Barcode Reader Connection and Operations")
        {
        }

        public BarcodeReaderException(string msg) : base(msg)
        {
        }
    }
}
