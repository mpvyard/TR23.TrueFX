// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

namespace TR23.TrueFX.WebJob.Protocol
{
    public static class WellKnown
    {
        public static class Queues
        {
            public const string FetchJobs = "fetchjobs";
            public const string PoisonInfos = "poisoninfos";
            public const string KickoffErrors = "kickofferrors";
        }

        public static class Containers
        {
            public const string TickSets = "ticksets";
        }
    }
}
