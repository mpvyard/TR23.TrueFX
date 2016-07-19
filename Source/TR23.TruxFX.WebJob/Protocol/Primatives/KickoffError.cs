// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System;
using System.ComponentModel.DataAnnotations;
using TR23.TrueFX.WebJob.Helpers;

namespace TR23.TrueFX.WebJob.Protocol
{
    public class KickoffError
    {
        [Required]
        public Guid KickoffErrorId { get; set; }

        [Required]
        public DateTime DetectedOn { get; set; }

        [Required]
        public Guid KickoffId { get; set; }

        [Required]
        public string Context { get; set; }

        [Required]
        public ErrorInfo ErrorInfo { get; set; }

        public override string ToString() =>
            $"{DetectedOn:yyyyMMddHHmmssfff}/{KickoffErrorId}";
    }
}
