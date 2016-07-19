// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

namespace TR23.TrueFX.WebJob.TickData
{
    public static class SymbolExtenders
    {
        public static int ToDigits(this Symbol symbol)
        {
            switch (symbol)
            {
                case Symbol.AUDJPY:
                case Symbol.CADJPY:
                case Symbol.CHFJPY:
                case Symbol.EURJPY:
                case Symbol.GBPJPY:
                case Symbol.USDJPY:
                    return 3;
                default:
                    return 5;
            }
        }

        public static double ToMaxRate(this Symbol symbol) =>
            symbol.ToDigits() == 3 ? 999.999 : 9.99999;
    }
}
