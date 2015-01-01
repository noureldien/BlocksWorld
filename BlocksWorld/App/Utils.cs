using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlocksWorld
{
    public class Utils
    {       
        /// <summary>
        /// Write the sent object to the debug.
        /// </summary>
        /// <param name="obj"></param>
        public static void DebugLine(object obj)
        {
            System.Diagnostics.Debug.WriteLine(obj);
        }
    }
}
