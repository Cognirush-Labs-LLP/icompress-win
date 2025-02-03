using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core
{
    /// <summary>
    /// Stores output settings including compression information and others. 
    /// </summary>
    public partial class OutputSettings
    {
        [AutoNotify]
        public int quality = 70;

        [AutoNotify]
        public OutputFormat format = OutputFormat.Jpg;


    }
}
