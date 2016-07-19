// Copyright (C) SquidEyes, LLC. - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and Confidential
// Written by Louis S. Berman <louis@squideyes.com>, 7/19/2016

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TR23.TrueFX.WebJob.Helpers;
using TR23.TrueFX.WebJob.Protocol;
using TR23.TrueFX.WebJob.TickData;
using static TR23.TrueFX.WebJob.Protocol.WellKnown;

namespace TR23.TrueFX.WebJob
{
    public class Functions
    {
        public static async Task Kickoff(
            [Queue(Queues.FetchJobs)] CloudQueue fetchJobs, TextWriter log,
            [Queue(Queues.KickoffErrors)] CloudQueue kickoffErrors,
            [Blob(Containers.TickSets)] CloudBlobContainer tickSets,
            CancellationToken token,
            [TimerTrigger("04:00:00", RunOnStartup = true)] TimerInfo info)
        {
            List<FetchJob> jobs = null;
            var kickoffId = Guid.NewGuid();

            try
            {
                // Remove comment to demonstrate HandleKickoffError():
                // throw new Exception("Ooops!");

                jobs = await GetFetchJobs(kickoffId, tickSets, token);
            }
            catch (Exception error)
            {
                await HandleKickoffError(log, error, kickoffId,
                    kickoffErrors, "GetFetchJobs()", token);

                return;
            }

            if (token.IsCancellationRequested)
                return;

            try
            {
                foreach (var job in jobs)
                {
                    if (token.IsCancellationRequested)
                        return;

                    var json = JsonConvert.SerializeObject(job);

                    await fetchJobs.AddMessageAsync(
                        new CloudQueueMessage(json), token);

                    if (token.IsCancellationRequested)
                        return;

                    await LogAddedToQueue(token,
                        log, fetchJobs, $"a \"{job}\" fetch-job");
                }

                await LogAddedToQueue(token,
                    log, fetchJobs, $"{jobs.Count:N0} fetch-jobs");
            }
            catch (Exception error)
            {
                await HandleKickoffError(log, error, kickoffId,
                    kickoffErrors, "AddMessageAsync()", token);
            }
        }

        private static async Task HandleKickoffError(TextWriter log,
            Exception error, Guid kickoffId, CloudQueue kickOffErrors,
            string context, CancellationToken token)
        {
            var kickoffError = new KickoffError()
            {
                KickoffErrorId = Guid.NewGuid(),
                DetectedOn = DateTime.UtcNow,
                KickoffId = kickoffId,
                Context = context,
                ErrorInfo = ErrorInfo.Create(error)
            };

            var json = JsonConvert.SerializeObject(
                kickoffError, Formatting.Indented);

            if (token.IsCancellationRequested)
                return;

            await kickOffErrors.AddMessageAsync(
                new CloudQueueMessage(json));

            await LogAddedToQueue(token, log, kickOffErrors,
                $"a \"{kickoffError}\" kickoff-error");
        }

        public static async Task SendKickoffErrorEmail(
            [SendGrid] SendGridMessage sendGrid, TextWriter log,
            CancellationToken token,
            [QueueTrigger(Queues.KickoffErrors)] KickoffError kickoffError)
        {
            const string FILENAME = "KickoffError.json";

            if (token.IsCancellationRequested)
                return;

            var json = JsonConvert
                .SerializeObject(kickoffError, Formatting.Indented);

            var subject = new StringBuilder();

            subject.Append($"[{typeof(Functions).Namespace} Kickoff Error] ");
            subject.Append($"(ID: {kickoffError.KickoffErrorId}, ");
            subject.Append($"Type: {kickoffError.ErrorInfo.ErrorType})");

            sendGrid.Subject = subject.ToString();

            var body = new StringBuilder();

            body.AppendLine("An error was detected while processing a Kickoff().");
            body.AppendLine($"See the \"{FILENAME}\" attachment for more details.");
            body.AppendLine();
            body.AppendLine($"KickoffId: {kickoffError.KickoffId}");
            body.AppendLine($"KickoffErrorId: {kickoffError.KickoffErrorId}");
            body.AppendLine(
                $"DetectedOn: {kickoffError.DetectedOn:MM/dd/yyyy HH:mm:ss.fff}");
            body.AppendLine($"Context: {kickoffError.Context}");
            body.AppendLine();
            body.AppendLine("ERROR DETAILS");
            body.AppendLine(JsonConvert.SerializeObject(
                kickoffError.ErrorInfo, Formatting.Indented));

            sendGrid.Text = body.ToString();

            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);

                writer.Write(json);

                writer.Flush();

                stream.Position = 0;

                sendGrid.AddAttachment(stream, FILENAME);
            }

            await log.WriteLineAsync(
                $"Sent KickoffError email to \"{sendGrid.To[0]}\"");
        }

        private static async Task LogAddedToQueue(CancellationToken token,
            TextWriter log, CloudQueue queue, string slug)
        {
            if (token.IsCancellationRequested)
                return;

            await log.WriteLineAsync(string.Format(
                "Added {0} ", slug) + $"to the \"{queue.Name}\" queue");
        }

        public static async Task ProcessFetchJob(CancellationToken token,
            [QueueTrigger(Queues.FetchJobs)] FetchJob job, TextWriter log,
            [Blob(Containers.TickSets)] CloudBlobContainer tickSets)
        {
            // Remove comment to show ErrorMonitor in action
            // throw new Exception("Ooops!");

            if (token.IsCancellationRequested)
                return;

            var client = new HttpClient();

            var response = await client.GetAsync(job.Uri, token);

            response.EnsureSuccessStatusCode();

            if (token.IsCancellationRequested)
                return;

            var stream = await response.Content.ReadAsStreamAsync();

            if (token.IsCancellationRequested)
                return;

            var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (token.IsCancellationRequested)
                    return;

                TickSet tickSet = null;

                using (var reader = new StreamReader(entry.Open()))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        var tick = Tick.Parse(line, "yyyyMMdd HH:mm:ss.fff");

                        if (tickSet == null)
                        {
                            tickSet = new TickSet(
                                tick.Symbol, tick.TickOn.Date);
                        }
                        else if (tickSet.Date != tick.Date)
                        {
                            if (token.IsCancellationRequested)
                                return;

                            await UploadTickSet(
                                job, tickSet, tickSets, token, log);

                            tickSet = new TickSet(
                                tick.Symbol, tick.TickOn.Date);
                        }

                        tickSet.Add(tick);
                    }
                }
            }
        }

        public static async Task ConvertCsvToZip(
            [BlobTrigger(Containers.TickSets + "/{name}")] Stream input,
            [Blob(Containers.TickSets)] CloudBlobContainer container,
            string name, TextWriter log)
        {
            var entryName = Path.GetFileName(name);

            var blobName = new TickSet(entryName).GetBlobName(FileKind.CsvZip);

            using (var output = new MemoryStream())
            {
                using (var archive = new ZipArchive(
                    output, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry(entryName);

                    await input.CopyToAsync(entry.Open());
                }

                var blob = container
                    .GetBlockBlobReference(blobName);

                output.Position = 0;

                await blob.UploadFromStreamAsync(output);
            }

            await log.WriteLineAsync(
                $"Converted \"{entryName}\" to \"{blobName}\"");
        }

        public static async Task ConvertCsvToTickSet(
            [BlobTrigger(Containers.TickSets + "/{name}")] Stream input,
            [Blob(Containers.TickSets)] CloudBlobContainer container,
            string name, TextWriter log)
        {
            var entryName = Path.GetFileName(name);

            var tickSet = new TickSet(entryName);

            using (var reader = new StreamReader(input))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                    tickSet.Add(Tick.Parse(line));
            }

            var blobName = tickSet.GetBlobName(FileKind.TickSet);

            using (var output = new MemoryStream())
            {
                using (var archive = new ZipArchive(
                    output, ZipArchiveMode.Create, true))
                {
                    var entry = archive.CreateEntry(entryName);

                    using (var byteStream = new MemoryStream(
                        tickSet.GetBytes(FileKind.TickSet)))
                    {
                        await byteStream.CopyToAsync(entry.Open());
                    }
                }

                var blob = container
                    .GetBlockBlobReference(blobName);

                output.Position = 0;

                await blob.UploadFromStreamAsync(output);
            }

            await log.WriteLineAsync(
                $"Converted \"{entryName}\" to \"{blobName}\"");
        }

        private static async Task UploadTickSet(FetchJob job, TickSet tickSet,
            CloudBlobContainer tickSets, CancellationToken token, TextWriter log)
        {
            var bytes = tickSet.GetBytes(job.FileKind);

            var blobName = tickSet.GetBlobName(job.FileKind);

            var blob = tickSets.GetBlockBlobReference(blobName);

            await blob.UploadFromByteArrayAsync(
                bytes, 0, bytes.Length, token);

            if (token.IsCancellationRequested)
                return;

            await log.WriteLineAsync(
                $"Uploaded {tickSet.Count:N0} ticks to \"{blobName}\"");
        }

        private static async Task<List<FetchJob>> GetFetchJobs(
            Guid kickoffId, CloudBlobContainer tickSets, CancellationToken token)
        {
            var fetchJobs = new List<FetchJob>();

            var existing = await tickSets.GetBlobNames(
                TickSet.Source + "/", token);

            if (token.IsCancellationRequested)
                return fetchJobs;

            var firstYear = int.Parse(
                ConfigurationManager.AppSettings["FirstYear"]);

            var symbolsToFetch =
                ConfigurationManager.AppSettings["SymbolsToFetch"];

            var symbols = new List<Symbol>();

            foreach (var stf in symbolsToFetch.Split(','))
                symbols.Add(stf.ToEnum<Symbol>());

            var minDate = new DateTime(
                firstYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (int year = firstYear; year < DateTime.UtcNow.Year; year++)
            {
                for (int month = 1; month < 12; month++)
                {
                    foreach (var symbol in symbols)
                    {
                        bool mustFetch = false;

                        for (int day = 1; day <= DateTime
                            .DaysInMonth(year, month); day++)
                        {
                            if (token.IsCancellationRequested)
                                return fetchJobs;

                            var date = new DateTime(year, month,
                                day, 0, 0, 0, DateTimeKind.Utc);

                            switch (date.DayOfWeek)
                            {
                                case DayOfWeek.Saturday:
                                case DayOfWeek.Sunday:
                                    continue;
                            }

                            var blobName = new TickSet(symbol, date)
                                .GetBlobName(FileKind.Csv);

                            if (!existing.Contains(blobName))
                            {
                                mustFetch = true;

                                break;
                            }
                        }

                        if (mustFetch)
                        {
                            fetchJobs.Add(new FetchJob()
                            {
                                KickoffId = kickoffId,
                                FileKind = FileKind.Csv,
                                Symbol = symbol,
                                Year = year,
                                Month = month
                            });
                        }
                    }
                }
            }

            return fetchJobs;
        }

        public static void ErrorMonitor(
            [ErrorTrigger("0:30:00", 10, Throttle = "1:00:00")] TraceFilter filter,
            [SendGrid] SendGridMessage message)
        {
            var sb = new StringBuilder();

            sb.Append($"[{typeof(Functions).Namespace} ErrorMonitor] ");
            sb.Append($"{filter.Events.Count} events were detected");

            message.Subject = sb.ToString();

            message.Text = filter.GetDetailedMessage(5);
        }
    }
}
