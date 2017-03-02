﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGApi
{
    class WN8
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
    }
}
