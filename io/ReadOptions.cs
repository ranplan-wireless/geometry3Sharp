//  ****************************************************************************
//  Ranplan Wireless Network Design Ltd.
//  __________________
//   All Rights Reserved. [2022]
// 
//  NOTICE:
//  All information contained herein is, and remains the property of
//  Ranplan Wireless Network Design Ltd. and its suppliers, if any.
//  The intellectual and technical concepts contained herein are proprietary
//  to Ranplan Wireless Network Design Ltd. and its suppliers and may be
//  covered by U.S. and Foreign Patents, patents in process, and are protected
//  by trade secret or copyright law.
//  Dissemination of this information or reproduction of this material
//  is strictly forbidden unless prior written permission is obtained
//  from Ranplan Wireless Network Design Ltd.
// ****************************************************************************

using System.Threading;

namespace g3
{
    public class ReadOptions
    {
        public bool ReadMaterials { get; set; }

        public CancellationToken CancellationToken { get; set; }

        // format readers will inevitably have their own settings, we
        // can use this to pass arguments to them
        public CommandArgumentSet CustomFlags = new CommandArgumentSet();

        public ReadOptions()
        {
            ReadMaterials = false;
            CancellationToken = CancellationToken.None;
        }

        public static readonly ReadOptions Defaults = new ReadOptions { ReadMaterials = false };
    }
}