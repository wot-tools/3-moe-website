using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WGApi;

namespace MoeFetcher
{
    class Player
    {
        public int ID { get; set; }
        public PlayerInfo PlayerInfo { get; set; }
        public Moe[] Moes { get; set; }
        public WinrateRecord[] Winrates { get; set; }
        public int WN8 { get; set; }
    }
}
