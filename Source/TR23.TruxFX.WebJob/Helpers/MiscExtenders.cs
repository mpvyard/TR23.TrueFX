// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System;
using System.IO;
using System.Text;

namespace TR23.TrueFX.WebJob.Helpers
{
    public static class MiscExtenders
    {
        public static bool IsDefined<T>(this Enum value) =>
            Enum.IsDefined(typeof(T), value);

        public static T ToEnum<T>(this string value) =>
            (T)Enum.Parse(typeof(T), value, true);

        public static string ToSingleLine(
            this string value, string delimiter = "; ")
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder();

            using (var reader = new StringReader(value))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (sb.Length > 0)
                        sb.Append(delimiter);

                    sb.Append(line.Trim());
                }
            }

            return sb.ToString();
        }
    }
}
