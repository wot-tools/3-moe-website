using System;

namespace MoeFetcher.WgApi
{
    class Player
    {
        public int ID { get; set; }
        public int? ClanID { get; set; }
        public string Nick { get; set; }
        public string ClientLanguage { get; set; }
        public DateTime LastLogout { get; set; }
        public DateTime AccountCreated { get; set; }
        public DateTime LastBattle { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Moe[] Moes { get; set; }
        public WinrateRecord[] WinrateRecords { get; set; }
        public Statistics Statistics { get; set; }
    }
}
