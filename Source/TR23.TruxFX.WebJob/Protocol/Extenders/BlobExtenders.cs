// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TR23.TrueFX.WebJob.Protocol
{
    public static class BlobHelpers
    {
        public static async Task<HashSet<string>> GetBlobNames(
            this CloudBlobContainer container, string prefix, 
            CancellationToken cancellationToken)
        {
            var blobNames = new HashSet<string>();

            BlobContinuationToken continuationToken = null;
            BlobResultSegment segment = null;

            do
            {
                segment = await container.ListBlobsSegmentedAsync(
                    prefix, true, BlobListingDetails.All, 5000,
                    continuationToken, null, null);

                foreach (var blobItem in segment.Results)
                {
                    if (blobItem is CloudBlockBlob)
                        blobNames.Add((blobItem as CloudBlockBlob).Name);
                }

                continuationToken = segment.ContinuationToken;
            }
            while (continuationToken != null);

            return blobNames;
        }
    }
}
