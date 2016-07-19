// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System;
using System.IO;
using System.Text;
using TR23.TrueFX.WebJob.Helpers;

namespace TR23.TrueFX.WebJob.TickData
{
    public class Tick
    {
        private static readonly DateTime MinTickOn =
            new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private Tick()
        {
        }

        public Tick(Symbol symbol, DateTime tickOn,
            double bidRate, double askRate)
        {
            Symbol = symbol;
            TickOn = tickOn;
            BidRate = bidRate;
            AskRate = askRate;

            Validate();
        }

        public Symbol Symbol { get; private set; }
        public DateTime TickOn { get; private set; }
        public double BidRate { get; private set; }
        public double AskRate { get; private set; }

        public DateTime Date => TickOn.Date;

        public string ToCsvString(
            bool includeSymbol = true, bool formatRates = true,
            string tickOnFormat = "MM/dd/yyyy HH:mm:ss.fff")
        {
            var sb = new StringBuilder();

            if (includeSymbol)
            {
                sb.Append(Symbol);
                sb.Append(',');
            }

            sb.Append(TickOn.ToString(tickOnFormat));
            sb.Append(',');

            if (formatRates)
            {
                var digits = Symbol.ToDigits();

                sb.Append(Math.Round(BidRate, digits));
                sb.Append(',');
                sb.Append(Math.Round(AskRate, digits));
            }
            else
            {
                sb.Append(BidRate);
                sb.Append(',');
                sb.Append(AskRate);
            }

            return sb.ToString();
        }

        public void Validate()
        {
            if (!Symbol.IsDefined<Symbol>())
                throw new ArgumentOutOfRangeException(nameof(Symbol));

            if (TickOn.Kind != DateTimeKind.Utc)
                throw new ArgumentOutOfRangeException(nameof(TickOn));

            if (BidRate <= 0.0 || BidRate > Symbol.ToMaxRate())
                throw new ArgumentOutOfRangeException(nameof(BidRate));

            if (AskRate <= 0.0 || AskRate > Symbol.ToMaxRate())
                throw new ArgumentOutOfRangeException(nameof(AskRate));
        }

        public void Write(BinaryWriter writer)
        {
            var bytes = BitConverter.GetBytes(
                (long)(TickOn - MinTickOn).TotalMilliseconds);

            writer.Write((short)Symbol);
            writer.Write(bytes, 0, 6);
            writer.Write((float)BidRate);
            writer.Write((float)AskRate);
        }

        public static Tick Read(
            BinaryReader reader, bool validate = false)
        {
            var tick = new Tick();

            tick.Symbol = (Symbol)reader.ReadInt16();

            var digits = tick.Symbol.ToDigits();

            var bytes = new byte[8];

            Array.Copy(reader.ReadBytes(6), bytes, 6);

            var ms = BitConverter.ToInt64(bytes, 0);

            tick.TickOn = MinTickOn.AddMilliseconds(ms);

            tick.BidRate = Math.Round(
                (double)reader.ReadSingle(), digits);

            tick.AskRate = Math.Round(
                (double)reader.ReadSingle(), digits);

            if (validate)
                tick.Validate();

            return tick;
        }

        public static Tick Parse(string line, string format = null)
        {
            var fields = line.Split(',');

            DateTime tickOn;

            if (format == null)
            {
                tickOn = DateTime.Parse(fields[1]);
            }
            else
            {
                tickOn = DateTime.ParseExact(
                    fields[1], format, null);
            }

            var symbol = fields[0].Replace("/", "").ToEnum<Symbol>();

            var digits = symbol.ToDigits();

            return new Tick()
            {
                Symbol = symbol,
                TickOn = DateTime.SpecifyKind(tickOn, DateTimeKind.Utc),
                BidRate = Math.Round(double.Parse(fields[2]), digits),
                AskRate = Math.Round(double.Parse(fields[3]), digits)
            };
        }
    }
}
