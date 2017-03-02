using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class Statistics
    {
        [JsonProperty("spotted")]
        public int Spotted { get; set; }
        [JsonProperty("battles")]
        public int Battles { get; set; }
        [JsonProperty("wins")]
        public int Victories { get; set; }
        [JsonProperty("damage_dealt")]
        public int Damage { get; set; }
        [JsonProperty("xp")]
        public int Experience { get; set; }
        [JsonProperty("frags")]
        public int Frags { get; set; }
        [JsonProperty("survived_battles")]
        public int SurvivedBattles { get; set; }
        [JsonProperty("capture_points")]
        public int Cap { get; set; }
        [JsonProperty("dropped_capture_points")]
        public int Decap { get; set; }
        [JsonProperty("shots")]
        public int Shots { get; set; }
        [JsonProperty("hits")]
        public int Hits { get; set; }
        [JsonProperty("draws")]
        public int Draws { get; set; }
        [JsonProperty("damage_received")]
        public int DamageReveived { get; set; }

        [JsonIgnore]
        public double AvgSpotted { get { return SafelyDivide(Spotted, Battles); } }
        [JsonIgnore]
        public double Winrate { get { return SafelyDivide(Victories, Battles); } }
        [JsonIgnore]
        public double AvgDamage { get { return SafelyDivide(Damage, Battles); } }
        [JsonIgnore]
        public double AvgExperience { get { return SafelyDivide(Experience, Battles); } }
        [JsonIgnore]
        public double AvgFrags { get { return SafelyDivide(Frags, Battles); } }
        [JsonIgnore]
        public double AvgSurvivedBattles { get { return SafelyDivide(SurvivedBattles, Battles); } }
        [JsonIgnore]
        public double AvgCap { get { return SafelyDivide(Cap, Battles); } }
        [JsonIgnore]
        public double AvgDecap { get { return SafelyDivide(Decap, Battles); } }
        [JsonIgnore]
        public double AvgDamageReveived { get { return SafelyDivide(DamageReveived, Battles); } }
        [JsonIgnore]
        public double Hitrate { get { return SafelyDivide(Hits, Shots); } }

        public Statistics() { }

        public Statistics(Statistics statistics)
        {
            Spotted = statistics.Spotted;
            Battles = statistics.Battles;
            Victories = statistics.Victories;
            Damage = statistics.Damage;
            Experience = statistics.Experience;
            Frags = statistics.Frags;
            SurvivedBattles = statistics.SurvivedBattles;
            Cap = statistics.Cap;
            Decap = statistics.Decap;
            Shots = statistics.Shots;
            Hits = statistics.Hits;
            Draws = statistics.Draws;
            DamageReveived = statistics.DamageReveived;
        }

        public static Statistics operator +(Statistics first, Statistics second)
        {
            return Operate(first, second, (f, s) => f + s);
        }

        public static Statistics operator -(Statistics first, Statistics second)
        {
            return Operate(first, second, (f, s) => f - s);
        }


        private static Statistics Operate(Statistics first, Statistics second, Func<int, int, int> operation)
        {
            Statistics result = new Statistics
            {
                Spotted = operation(first.Spotted, second.Spotted),
                Battles = operation(first.Battles, second.Battles),
                Victories = operation(first.Victories, second.Victories),
                Damage = operation(first.Damage, second.Damage),
                Experience = operation(first.Experience, second.Experience),
                Frags = operation(first.Frags, second.Frags),
                SurvivedBattles = operation(first.SurvivedBattles, second.SurvivedBattles),
                Cap = operation(first.Cap, second.Cap),
                Decap = operation(first.Decap, second.Decap),
                Shots = operation(first.Shots, second.Shots),
                Hits = operation(first.Hits, second.Hits),
                Draws = operation(first.Draws, second.Draws),
                DamageReveived = operation(first.DamageReveived, second.DamageReveived),
            };
            return result;
        }

        private double SafelyDivide(int dividend, int divisor)
        {
            if (divisor == 0)
                return 0;
            return dividend / (double)divisor;
        }

        public double CalculateWN8(ExpectedValues expectedValues)
        {
            return WN8.Calculate(AvgDamage, AvgSpotted, AvgFrags, AvgDecap, Winrate, expectedValues);
        }
    }
}
