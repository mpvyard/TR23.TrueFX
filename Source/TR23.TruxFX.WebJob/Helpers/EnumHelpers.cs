// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using System;
using System.Collections.Generic;
using System.Linq;

namespace TR23.TrueFX.WebJob.Helpers
{
    public static class EnumHelpers
    {
        public static List<T> ToList<T>() =>
            Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }
}
