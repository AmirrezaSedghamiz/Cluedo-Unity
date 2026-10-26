using System;
using System.Collections.Generic;
using System.Diagnostics;
using TomorrowsCrime.Procedural.Core;
using TomorrowsCrime.Procedural.Mystery;

namespace TomorrowsCrime.Procedural.Validation
{
    public class BatchValidationReport
    {
        public int TotalSeedsTested { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public double AverageMsPerSeed => TotalSeedsTested > 0 ? ElapsedMilliseconds / TotalSeedsTested : 0;
        public List<string> FailureLogs { get; } = new();

        public override string ToString()
        {
            return $"[BatchValidation] Tested: {TotalSeedsTested}, Passed: {PassedCount} ({(PassedCount * 100.0 / TotalSeedsTested):F1}%), Failed: {FailedCount}, Total Time: {ElapsedMilliseconds:F1}ms (Avg: {AverageMsPerSeed:F2}ms/seed)";
        }
    }

    /// <summary>
    /// Automated multi-seed stress testing and validation utility.
    /// Runs a batch of 100+ seeds through the generation and solvability validation pipeline.
    /// </summary>
    public static class MultiSeedBatchRunner
    {
        public static BatchValidationReport RunBatchTest(int seedCount = 100, int startingSeed = 1000)
        {
            var report = new BatchValidationReport
            {
                TotalSeedsTested = seedCount
            };

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < seedCount; i++)
            {
                int currentSeedValue = startingSeed + i;
                var seed = new InvestigationSeed(currentSeedValue);

                try
                {
                    var investigation = GenerationPipeline.GenerateInvestigation(seed, validateSolvability: true);

                    // Assertions on generated output
                    bool passed = true;

                    if (investigation.Rooms == null || investigation.Rooms.Count < 10)
                    {
                        report.FailureLogs.Add($"Seed {currentSeedValue}: Room count ({investigation.Rooms?.Count ?? 0}) < 10.");
                        passed = false;
                    }

                    if (investigation.Roster == null || investigation.Roster.Count < 8)
                    {
                        report.FailureLogs.Add($"Seed {currentSeedValue}: NPC roster ({investigation.Roster?.Count ?? 0}) < 8.");
                        passed = false;
                    }

                    if (investigation.Validation == null || !investigation.Validation.IsValid)
                    {
                        report.FailureLogs.Add($"Seed {currentSeedValue}: Mystery validation failed: {investigation.Validation}");
                        passed = false;
                    }

                    if (investigation.GroundTruth == null || investigation.GroundTruth.CulpritId == investigation.GroundTruth.VictimId)
                    {
                        report.FailureLogs.Add($"Seed {currentSeedValue}: Invalid ground truth (culprit == victim).");
                        passed = false;
                    }

                    if (passed)
                    {
                        report.PassedCount++;
                    }
                    else
                    {
                        report.FailedCount++;
                    }
                }
                catch (Exception ex)
                {
                    report.FailedCount++;
                    report.FailureLogs.Add($"Seed {currentSeedValue}: Unhandled exception: {ex.Message}");
                }
            }

            sw.Stop();
            report.ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds;

            return report;
        }
    }
}
