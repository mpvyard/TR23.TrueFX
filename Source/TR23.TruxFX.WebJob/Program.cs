// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SendGrid;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Mail;
using static TR23.TrueFX.WebJob.Protocol.WellKnown;

namespace TR23.TrueFX.WebJob
{
    class Program
    {
        static void Main()
        {
            Console.SetWindowSize(100, 25);

            var watcher = new WebJobsShutdownWatcher();

            if (watcher.Token.IsCancellationRequested)
                return;

            PrepStorage();

            var config = new JobHostConfiguration();

            if (Environment.UserInteractive)
            {
                // Remove comment to ease debugging
                config.Queues.BatchSize = 1;

                config.UseDevelopmentSettings();
                config.Tracing.ConsoleLevel = TraceLevel.Verbose;
            }

            config.UseCore();

            config.UseTimers();

            config.UseSendGrid(new SendGridConfiguration()
            {
                FromAddress = new MailAddress(
                    ConfigurationManager.AppSettings["AlertsFrom"]),
                ToAddress = ConfigurationManager.AppSettings["AlertsTo"]
            });

            var host = new JobHost(config);

            if (!watcher.Token.IsCancellationRequested)
                host.RunAndBlock();
        }

        public static void PrepStorage()
        {
            var connString = AmbientConnectionStringProvider
                .Instance.GetConnectionString(
                ConnectionStringNames.Storage);

            var account = CloudStorageAccount.Parse(connString);

            var client = account.CreateCloudBlobClient();

            var container = client
                .GetContainerReference(Containers.TickSets);

            container.CreateIfNotExists();
        }
    }
}
