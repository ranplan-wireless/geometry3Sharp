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

using System.Collections.Generic;

namespace g3
{
    // ReSharper disable once InconsistentNaming
    public class MeshIOLogger
    {
        private Dictionary<string, int> warningCount = new Dictionary<string, int>();

        public int nWarningLevel = 0; // 0 == no diagnostics, 1 == basic, 2 == crazy

        // connect to this to get warning messages
        public event ParsingMessagesHandler warningEvent;

        public void emit_warning(string sMessage)
        {
            var sPrefix = sMessage.Substring(0, 15);
            var nCount = warningCount.ContainsKey(sPrefix) ? warningCount[sPrefix] : 0;
            nCount++;
            warningCount[sPrefix] = nCount;
            if (nCount > 10)
                return;
            else if (nCount == 10)
                sMessage += " (additional message surpressed)";

            var e = warningEvent;
            if (e != null)
                e(sMessage, null);
        }
    }
}