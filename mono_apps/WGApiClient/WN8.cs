using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    public class WN8
    {
        public static double Calculate(double damage, double spotted, double frags, double decap, double winrate, ExpectedValues expectedValues)
        {
            double rDamage = damage / expectedValues.Damage;
            double rSpot = spotted / expectedValues.Spots;
            double rFrag = frags / expectedValues.Frags;
            double rDef = decap / expectedValues.Defense;
            double rWin = winrate * 100 / expectedValues.Winrate;

            Func<double, double, double> normalize = (r, d) => (r - d) / (1 - d);

            double rWinC = Math.Max(0, normalize(rWin, 0.71));
            double rDamageC = Math.Max(0, normalize(rDamage, 0.22));
            double rFragC = Math.Max(0, Math.Min(rDamageC + 0.2, normalize(rFrag, 0.12)));
            double rSpotC = Math.Max(0, Math.Min(rDamageC + 0.1, normalize(rSpot, 0.38)));
            double rDefC = Math.Max(0, Math.Min(rDamageC + 0.1, normalize(rDef, 0.1)));

            return 980 * rDamageC + 210 * rDamageC * rFragC + 155 * rFragC * rSpotC + 75 * rDefC * rFragC + 145 * Math.Min(1.8, rWinC);
        }

        public static double EstimatedAccountWN8(ExpectedValueList expectedValueList, int version, WinrateRecord[] winrateRecords, Statistics cumulatedStats)
        {
            Dictionary<int, ExpectedValues> expectedValues = expectedValueList[version];
            ExpectedValues cumulatedExpected = new ExpectedValues();
            foreach (var winrateRecord in winrateRecords)
                if (expectedValues.TryGetValue(winrateRecord.TankID, out ExpectedValues values))
                {
                    cumulatedExpected += values * winrateRecord.Battles;
                }
            cumulatedExpected /= cumulatedStats.Battles;
            return cumulatedStats.CalculateWN8(cumulatedExpected);
        }

        public static double EstimatedAccountWN8Newest(ExpectedValueList expectedValueList, WinrateRecord[] winrateRecords, Statistics cumulatedStats)
        {
            return EstimatedAccountWN8(expectedValueList, expectedValueList.Versions.Max(), winrateRecords, cumulatedStats);
        }

        public static double AccountWN8(ExpectedValueList expectedValueList, int version, Dictionary<int, Statistics> tankStats)
        {
            Dictionary<int, ExpectedValues> expectedValues = expectedValueList[version];
            Statistics cumulatedStats = new Statistics();
            ExpectedValues cumulatedExpected = new ExpectedValues();
            foreach (var pair in tankStats)
                if (expectedValues.TryGetValue(pair.Key, out ExpectedValues values))
                {
                    cumulatedExpected += values * pair.Value.Battles;
                    cumulatedStats += pair.Value;
                }
            cumulatedExpected /= cumulatedStats.Battles;
            return cumulatedStats.CalculateWN8(cumulatedExpected);
        }

        public static double AccountWN8Newest(ExpectedValueList expectedValueList, Dictionary<int, Statistics> tankStats)
        {
            return AccountWN8(expectedValueList, expectedValueList.Versions.Max(), tankStats);
        }
    }
}
