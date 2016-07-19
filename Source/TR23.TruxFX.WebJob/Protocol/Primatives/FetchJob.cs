// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TR23.TrueFX.WebJob.TickData;

namespace TR23.TrueFX.WebJob.Protocol
{
    public class FetchJob
    {
        private const string BASEURI = "http://www.truefx.com/";

        [Required]
        public Guid KickoffId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Symbol Symbol { get; set; }

        [Required]
        [Range(2000, 2040)]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FileKind FileKind { get; set; }

        public override string ToString() =>
            $"{Month:00}/{Year};{Symbol};{FileKind}";

        public Uri Uri
        {
            get
            {
                var sb = new StringBuilder();

                var monthName = new DateTime(
                    Year, Month, 1).ToString("MMMM");

                sb.Append(BASEURI);
                sb.Append("dev/data/");
                sb.Append(Year);
                sb.Append('/');
                sb.Append(monthName.ToUpper());
                sb.Append('-');
                sb.Append(Year);
                sb.Append('/');
                sb.Append(Symbol);
                sb.Append('-');
                sb.Append(Year);
                sb.Append('-');
                sb.AppendFormat("{0:00}", Month);
                sb.Append(".zip");

                return new Uri(sb.ToString());
            }
        }
    }
}
