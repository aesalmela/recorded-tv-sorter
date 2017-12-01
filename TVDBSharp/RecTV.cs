using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVDBSharp
{
    public class RecTV
    {
        public string filePath { get; set; }
        public string parentDir { get; set; }
        public string fileName { get; set; }
        public string fileExt { get; set; }
        public string title { get; set; }
        public string epID { get; set; }
        public string epName { get; set; }
        public string epDesc { get; set; }
        public DateTime recDateTime { get; set; }
        public bool sortable { get; set; }

        public RecTV(string showPath)
        {
            filePath = showPath;
        }
    }
}

    
