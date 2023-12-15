using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionLauncher
{
    public class Game
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string InstalledPath { get; set; }
        public int RunCount { get; set; }
        public DateTime LastRunDateTime { get; set; }
    }
}
