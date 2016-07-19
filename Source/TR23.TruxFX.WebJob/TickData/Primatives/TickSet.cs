// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using TR23.TrueFX.WebJob.Helpers;

namespace TR23.TrueFX.WebJob.TickData
{
    public class TickSet : IEnumerable<Tick>
    {
        public const string Source = "TRUEFX";
        public const int VERSION = 1;

        private List<Tick> ticks = new List<Tick>();

        public TickSet(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentNullException(nameof(blobName));

            var fields = blobName.Split('_');

            if (fields.Length != 6)
                throw new ArgumentOutOfRangeException(nameof(blobName));

            if(fields[0] != Source)
                throw new ArgumentOutOfRangeException(nameof(blobName));

            Symbol symbol;

            if(!Enum.TryParse(fields[1], out symbol))
                throw new ArgumentOutOfRangeException(nameof(blobName));

            int year = int.Parse(fields[2]);
            int month = int.Parse(fields[3]);
            int day = int.Parse(fields[4]);

            Symbol = symbol;
            Date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        public TickSet(Symbol symbol, DateTime date)
        {
            if (!symbol.IsDefined<Symbol>())
                throw new ArgumentOutOfRangeException(nameof(symbol));

            if (date.TimeOfDay != TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(date));

            if (date.Kind != DateTimeKind.Utc)
                throw new ArgumentOutOfRangeException(nameof(date));

            Symbol = symbol;
            Date = date;
        }

        public Symbol Symbol { get; }
        public DateTime Date { get; }

        public int Count => ticks.Count;

        public string GetBlobName(FileKind fileKind)
        {
            var kind = fileKind.ToString().ToUpper();

            var sb = new StringBuilder();

            sb.Append($"{Source}/{Symbol}/{kind}/{Date:yyyy/MM}/");

            sb.Append($"{Source}_{Symbol}_{Date:yyyy_MM_dd}_UTC");

            switch (fileKind)
            {
                case FileKind.TickSet:
                    sb.Append(".tickset");
                    break;
                case FileKind.CsvZip:
                    sb.Append(".zip");
                    break;
                default:
                    sb.Append(".csv");
                    break;
            }

            return sb.ToString();
        }

        public void Add(Tick tick)
        {
            tick.Validate();

            if (tick.Symbol != Symbol)
                throw new ArgumentOutOfRangeException(nameof(Symbol));

            if (tick.Date != Date)
                throw new ArgumentOutOfRangeException(nameof(Date));

            ticks.Add(tick);
        }

        public byte[] GetBytes(FileKind fileKind)
        {
            var sb = new StringBuilder();

            foreach (var tick in ticks)
                sb.AppendLine(tick.ToCsvString());

            switch (fileKind)
            {
                case FileKind.Csv:
                    return Encoding.UTF8.GetBytes(sb.ToString());
                case FileKind.CsvZip:
                    return GetCsvZipBytes(sb.ToString());
                default:
                    return GetTickSetBytes();
            }
        }

        private byte[] GetCsvZipBytes(string data)
        {
            byte[] bytes;

            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream))
                {
                    var entry = archive.CreateEntry(
                        $"{Source}_{Symbol}_{Date:yyyy_MM_dd}_UTC.csv");

                    using (var writer = new StreamWriter(entry.Open()))
                        writer.Write(data);
                }

                bytes = stream.ToArray();
            }

            return bytes;
        }

        private byte[] GetTickSetBytes()
        {
            byte[] bytes;

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(VERSION);
                    writer.Write((int)Symbol);
                    writer.Write(Date.Ticks);
                    writer.Write(ticks.Count);
                    writer.Write(new byte[44]);

                    foreach (var tick in ticks)
                        tick.Write(writer);
                };

                bytes = stream.ToArray();
            }

            return bytes;
        }

        public IEnumerator<Tick> GetEnumerator() =>
            ticks.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
