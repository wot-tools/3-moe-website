using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFetcher.WgApi
{
    class Clan
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int Count { get; set; }
        public string Color { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Emblems Emblems { get; set; }
    }
}
