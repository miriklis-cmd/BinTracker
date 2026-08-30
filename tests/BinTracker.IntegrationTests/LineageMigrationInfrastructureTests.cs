using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class LineageMigrationInfrastructureTests
{
    [Fact]
    public async Task Runtime_and_upgrade_leases_are_database_scoped_exclusive_and_releasable()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        var otherPath = Path.Combine(fixture.Root, "other.db");
        File.Copy(fixture.DatabasePath, otherPath);
        var before = await HashAsync(fixture.DatabasePath);
        var gate = CreateGate(fixture);

        using (var first = gate.AcquireRuntime(fixture.DatabasePath))
        using (var second = gate.AcquireRuntime(fixture.DatabasePath))
        {
            Assert.Equal(Path.GetFullPath(fixture.DatabasePath), first.DatabasePath);
            Assert.Equal(first.GateIdentity, second.GateIdentity);

            var contention = await Task.Run(() =>
                Assert.Throws<DatabaseUpgradeUnavailableException>((Action)(() =>
                    gate.AcquireUpgrade(fixture.DatabasePath))));
            Assert.Contains("upgrade gate", contention.Message, StringComparison.OrdinalIgnoreCase);

            using var independent = gate.AcquireUpgrade(otherPath);
            Assert.NotEqual(first.GateIdentity, independent.GateIdentity);
        }

        using var reacquired = gate.AcquireUpgrade(fixture.DatabasePath);
        Assert.True(reacquired.PendingOperationCheckPassed);
        Assert.Throws<DatabaseUpgradeUnavailableException>((Action)(() =>
            gate.AcquireRuntime(fixture.DatabasePath)));
        Assert.Equal(before, await HashAsync(fixture.DatabasePath));
    }

    [Fact]
    public async Task Upgrade_gate_disposal_after_exception_releases_ownership()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        var gate = CreateGate(fixture);

        Assert.Throws<ExpectedTestException>((Action)(() =>
        {
            using var lease = gate.AcquireUpgrade(fixture.DatabasePath);
            throw new ExpectedTestException();
        }));

        using var reacquired = gate.AcquireUpgrade(fixture.DatabasePath);
        Assert.NotNull(reacquired);
    }

    [Fact]
    public async Task Pending_database_operation_blocks_upgrade_but_not_runtime_participation()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        var marker = Path.Combine(fixture.BaseRoot, "pending.json");
        await File.WriteAllTextAsync(marker, "pending");
        var gate = new WindowsFileDatabaseUpgradeGate(
            Path.Combine(fixture.BaseRoot, "locks"),
            new PendingDatabaseOperationConflictProbe(marker));

        using var runtime = gate.AcquireRuntime(fixture.DatabasePath);
        runtime.Dispose();
        var error = Assert.Throws<DatabaseUpgradeUnavailableException>(() =>
            gate.AcquireUpgrade(fixture.DatabasePath));

        Assert.Equal(DatabaseUpgradeUnavailableReason.PendingDatabaseOperation, error.Reason);
        File.Delete(marker);
        using var upgrade = gate.AcquireUpgrade(fixture.DatabasePath);
        Assert.True(upgrade.PendingOperationCheckPassed);
    }

    [Fact]
    public async Task Hard_link_alias_uses_same_file_identity_and_cannot_bypass_gate()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        var alias = Path.Combine(fixture.Root, "hard-link-alias.db");
        Assert.True(CreateHardLink(alias, fixture.DatabasePath, IntPtr.Zero));
        var gate = CreateGate(fixture);

        using var runtime = gate.AcquireRuntime(fixture.DatabasePath);
        var error = Assert.Throws<DatabaseUpgradeUnavailableException>((Action)(() =>
            gate.AcquireUpgrade(alias)));

        Assert.Equal(DatabaseUpgradeUnavailableReason.ConflictingDatabaseUse, error.Reason);
    }

    [Fact]
    public async Task Normalized_path_alias_uses_the_same_database_gate()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        var alias = Path.Combine(fixture.Root, ".", Path.GetFileName(fixture.DatabasePath));
        var gate = CreateGate(fixture);

        using var runtime = gate.AcquireRuntime(fixture.DatabasePath);
        var error = Assert.Throws<DatabaseUpgradeUnavailableException>((Action)(() =>
            gate.AcquireUpgrade(alias)));

        Assert.Equal(DatabaseUpgradeUnavailableReason.ConflictingDatabaseUse, error.Reason);
    }

    [Fact]
    public async Task Child_process_runtime_lease_blocks_upgrade_until_process_releases()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        var gate = CreateGate(fixture);
        string lockPath;
        using (var lease = gate.AcquireRuntime(fixture.DatabasePath))
            lockPath = lease.GateIdentity;

        using var process = StartChildRuntimeLease(lockPath);
        var ready = await process.StandardOutput.ReadLineAsync()
            .WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("READY", ready);

        Assert.Throws<DatabaseUpgradeUnavailableException>((Action)(() =>
            gate.AcquireUpgrade(fixture.DatabasePath)));

        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        using var acquired = gate.AcquireUpgrade(fixture.DatabasePath);
        Assert.NotNull(acquired);
    }

    [Fact]
    public async Task Preflight_reads_supported_structural_lineage_without_modifying_database()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        var before = await HashAsync(fixture.DatabasePath);

        var result = await new SqliteLineageMigrationPreflight()
            .InspectAsync(fixture.DatabasePath);

        Assert.Equal(LineagePreflightClassification.Migratable, result.Classification);
        Assert.Empty(result.Issues);
        Assert.Equal(SqliteLineageMigrationPreflight.ExpectedSourceSchemaVersion, result.SchemaVersion);
        Assert.Equal(9, result.Counts.Movements);
        Assert.Equal(1, result.Counts.MovementBatches);
        Assert.Equal(2, result.Counts.CorrectionOperations);
        Assert.Equal(2, result.Counts.CorrectionLines);
        Assert.Equal(1, result.Counts.OrdinaryReversals);
        Assert.Equal(1, result.Counts.ImportOwnedMovements);
        Assert.Equal(1, result.Counts.AdjustmentMovements);
        Assert.Equal(2, result.Counts.SingleCorrections);
        Assert.Equal(0, result.Counts.WholeBatchCorrections);
        Assert.Equal(1, result.Counts.RepeatedCorrections);
        Assert.Equal(1, result.Counts.PartiallyReversedBatches);
        Assert.Equal(9, result.TableRowCounts["BinMovements"]);
        Assert.Equal(2, result.TableRowCounts["MovementCorrectionLines"]);
        Assert.True(result.IntegrityCheckPassed);
        Assert.True(result.ForeignKeyCheckPassed);
        Assert.Equal(before, await HashAsync(fixture.DatabasePath));
    }

    [Fact]
    public async Task Preflight_fails_closed_on_contradictory_correction_relationship()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        await fixture.ExecuteAsync(
            "UPDATE BinMovements SET CorrectedByMovementId=NULL WHERE Id=1;");

        var result = await new SqliteLineageMigrationPreflight()
            .InspectAsync(fixture.DatabasePath);

        Assert.Equal(LineagePreflightClassification.Invalid, result.Classification);
        Assert.Contains(result.Issues, x =>
            x.ReasonCode == LineagePreflightReasonCode.CorrectedByRelationshipMismatch &&
            x.EntityType == "BinMovement" &&
            x.EntityId == 1);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(99)]
    public async Task Preflight_blocks_undefined_schema_16_operation_kind(int unsupportedKind)
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        await fixture.ExecuteAsync(
            $"PRAGMA ignore_check_constraints=ON; UPDATE MovementCorrectionOperations SET Kind={unsupportedKind} WHERE Id=1;");

        var result = await new SqliteLineageMigrationPreflight()
            .InspectAsync(fixture.DatabasePath);

        Assert.Equal(LineagePreflightClassification.GlobalBlocker, result.Classification);
        Assert.Contains(result.Issues, x =>
            x.ReasonCode == LineagePreflightReasonCode.UnsupportedCorrectionKind &&
            x.EntityType == "MovementCorrectionOperation" &&
            x.EntityId == 1);
    }

    [Fact]
    public async Task Preflight_blocks_import_owned_generic_lineage()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        await fixture.ExecuteAsync(
            "UPDATE BinMovements SET ImportRunId=1, Source=2 WHERE Id=1;");

        var result = await new SqliteLineageMigrationPreflight()
            .InspectAsync(fixture.DatabasePath);

        Assert.Equal(LineagePreflightClassification.GlobalBlocker, result.Classification);
        Assert.Contains(result.Issues, x =>
            x.ReasonCode == LineagePreflightReasonCode.CrossDomainLineage);
    }

    [Fact]
    public async Task Preflight_rejects_untruthful_whole_batch_relationship()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        await fixture.ExecuteAsync(
            "UPDATE MovementCorrectionOperations SET Kind=1, OriginalBatchId=1, ReplacementBatchId=1 WHERE Id=1;");

        var result = await new SqliteLineageMigrationPreflight()
            .InspectAsync(fixture.DatabasePath);

        Assert.Equal(LineagePreflightClassification.Invalid, result.Classification);
        Assert.Contains(result.Issues, x =>
            x.ReasonCode == LineagePreflightReasonCode.InvalidPhysicalBatchRelationship &&
            x.EntityId == 1);
    }

    [Fact]
    public async Task Preflight_accepts_truthful_whole_batch_physical_relationship()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedWholeBatchCorrectionAsync();

        var result = await new SqliteLineageMigrationPreflight()
            .InspectAsync(fixture.DatabasePath);

        Assert.Equal(LineagePreflightClassification.Migratable, result.Classification);
        Assert.Equal(1, result.Counts.WholeBatchCorrections);
        Assert.Equal(1, result.Counts.CorrectionLines);
    }

    [Fact]
    public async Task Verified_backup_uses_frozen_name_manifest_checksums_and_source_binding()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        var backupFolder = Path.Combine(fixture.BaseRoot, "recovery", "lineage");
        var preflight = new SqliteLineageMigrationPreflight();
        var gate = CreateGate(fixture);
        var fixedTime = new DateTimeOffset(2026, 8, 30, 3, 15, 0, 123, TimeSpan.Zero);
        var service = new SqliteLineageMigrationBackupService(
            gate,
            preflight,
            new LineageMigrationBackupNameSource(
                new FixedTimeProvider(fixedTime),
                () => Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000")),
            () => Guid.Parse("11111111-2222-3333-4444-555555555555"),
            new FixedTimeProvider(fixedTime));

        using var upgradeLease = gate.AcquireUpgrade(fixture.DatabasePath);
        var result = await service.CreateVerifiedAsync(upgradeLease, backupFolder);
        var verification = await service.VerifyForSourceAsync(
            result.ManifestPath,
            fixture.DatabasePath);
        var sourceResult = await preflight.InspectAsync(fixture.DatabasePath);
        var backupResult = await preflight.InspectAsync(result.BackupPath);

        Assert.Equal(
            "BinTracker-pre-lineage-v16-20260830T031500123Z-a1b2c3d4.db",
            Path.GetFileName(result.BackupPath));
        Assert.True(File.Exists(result.BackupPath));
        Assert.True(new FileInfo(result.BackupPath).Length > 0);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.ChecksumPath));
        Assert.True(verification.IsValidForExpectedSource);
        Assert.Null(verification.FailureCode);
        Assert.Equal(LineageMigrationBackupPolicy.ManifestFormatVersion, result.Manifest.FormatVersion);
        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), result.Manifest.ArtifactId);
        Assert.Equal(LineageMigrationBackupPolicy.Purpose, result.Manifest.Purpose);
        Assert.StartsWith("0.5.0-alpha.8.7", result.Manifest.ApplicationInformationalVersion, StringComparison.Ordinal);
        Assert.Equal("SQLite", result.Manifest.Provider);
        Assert.Equal(Path.GetFullPath(fixture.DatabasePath), result.Manifest.SourceDatabasePath);
        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.SourcePathIdentityHash));
        Assert.StartsWith("WIN32:", result.Manifest.SourceFileIdentity, StringComparison.Ordinal);
        Assert.Equal(SqliteLineageMigrationPreflight.ExpectedSourceSchemaVersion, result.Manifest.SourceSchemaVersion);
        Assert.True(result.Manifest.SourceDatabaseSize > 0);
        Assert.NotEqual(default, result.Manifest.SourceLastWriteUtc);
        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.SourceJournalMode));
        Assert.Equal(new FileInfo(result.BackupPath).Length, result.Manifest.BackupSize);
        Assert.Equal("ok", result.Manifest.IntegrityCheckResult);
        Assert.Equal("ok", result.Manifest.ForeignKeyCheckResult);
        Assert.Equal(LineagePreflightClassification.Migratable, result.Manifest.PreflightClassification);
        Assert.Equal(LineageMigrationBackupPolicy.RecoveryPolicyId, result.Manifest.RecoveryPolicyId);
        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.RecoveryInstructions));
        Assert.Equal(sourceResult.Counts, backupResult.Counts);
        Assert.Equal(
            sourceResult.TableRowCounts.OrderBy(x => x.Key),
            backupResult.TableRowCounts.OrderBy(x => x.Key));
        Assert.Equal(sourceResult.StructuralFingerprint, backupResult.StructuralFingerprint);
        Assert.Equal(await HashAsync(result.BackupPath), result.Manifest.BackupSha256);

        var checksum = JsonSerializer.Deserialize<LineageMigrationChecksumEvidence>(
            await File.ReadAllTextAsync(result.ChecksumPath));
        Assert.NotNull(checksum);
        Assert.Equal(result.Manifest.ArtifactId, checksum.ArtifactId);
        Assert.Equal(result.Manifest.BackupSha256, checksum.BackupSha256);
        Assert.Equal(await HashAsync(result.ManifestPath), checksum.ManifestSha256);
    }

    [Fact]
    public async Task Atomic_publish_never_overwrites_an_existing_candidate()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        var backupFolder = Path.Combine(fixture.BaseRoot, "recovery");
        Directory.CreateDirectory(backupFolder);
        var occupiedName = "BinTracker-pre-lineage-v16-20260830T031500123Z-aaaaaaaa.db";
        var selectedName = "BinTracker-pre-lineage-v16-20260830T031500124Z-bbbbbbbb.db";
        var occupiedPath = Path.Combine(backupFolder, occupiedName);
        var sentinel = new byte[] { 7, 6, 5, 4 };
        await File.WriteAllBytesAsync(occupiedPath, sentinel);
        var service = new SqliteLineageMigrationBackupService(
            CreateGate(fixture),
            new SqliteLineageMigrationPreflight(),
            new QueueNameSource(occupiedName, selectedName));

        var result = await service.CreateVerifiedAsync(fixture.DatabasePath, backupFolder);

        Assert.Equal(selectedName, Path.GetFileName(result.BackupPath));
        Assert.Equal(sentinel, await File.ReadAllBytesAsync(occupiedPath));
    }

    [Fact]
    public async Task Database_manifest_and_checksum_tampering_fail_closed()
    {
        using var fixture = await DatabaseFixture.CreateAsync();
        await fixture.SeedLineageShapesAsync();
        var service = CreateBackupService(fixture);

        var backupOne = await service.CreateVerifiedAsync(
            fixture.DatabasePath,
            Path.Combine(fixture.BaseRoot, "backup-one"));
        await ToggleFirstByteAsync(backupOne.BackupPath);
        var tamperedBackup = await service.VerifyForSourceAsync(
            backupOne.ManifestPath,
            fixture.DatabasePath);
        Assert.False(tamperedBackup.IsValidForExpectedSource);
        Assert.Equal("BACKUP_HASH_MISMATCH", tamperedBackup.FailureCode);

        var backupTwo = await service.CreateVerifiedAsync(
            fixture.DatabasePath,
            Path.Combine(fixture.BaseRoot, "backup-two"));
        var json = await File.ReadAllTextAsync(backupTwo.ManifestPath);
        await File.WriteAllTextAsync(backupTwo.ManifestPath, json + " ");
        var tamperedManifest = await service.VerifyForSourceAsync(
            backupTwo.ManifestPath,
            fixture.DatabasePath);
        Assert.False(tamperedManifest.IsValidForExpectedSource);
        Assert.Equal("MANIFEST_HASH_MISMATCH", tamperedManifest.FailureCode);

        var backupThree = await service.CreateVerifiedAsync(
            fixture.DatabasePath,
            Path.Combine(fixture.BaseRoot, "backup-three"));
        await File.WriteAllTextAsync(backupThree.ChecksumPath, "{}");
        var tamperedChecksum = await service.VerifyForSourceAsync(
            backupThree.ManifestPath,
            fixture.DatabasePath);
        Assert.False(tamperedChecksum.IsValidForExpectedSource);
        Assert.Equal("CHECKSUM_EVIDENCE_INVALID", tamperedChecksum.FailureCode);

        var backupFour = await service.CreateVerifiedAsync(
            fixture.DatabasePath,
            Path.Combine(fixture.BaseRoot, "backup-four"));
        await using (var empty = new FileStream(
            backupFour.BackupPath,
            FileMode.Truncate,
            FileAccess.Write,
            FileShare.None))
        {
        }
        var emptyBackup = await service.VerifyForSourceAsync(
            backupFour.ManifestPath,
            fixture.DatabasePath);
        Assert.False(emptyBackup.IsValidForExpectedSource);
        Assert.Equal("BACKUP_SIZE_MISMATCH", emptyBackup.FailureCode);
    }

    [Fact]
    public async Task Backup_is_bound_to_exact_source_and_swapped_artifacts_are_rejected()
    {
        using var fixtureA = await DatabaseFixture.CreateAsync();
        using var fixtureB = await DatabaseFixture.CreateAsync();
        await fixtureA.SeedLineageShapesAsync();
        await fixtureB.SeedWholeBatchCorrectionAsync();
        var serviceA = CreateBackupService(fixtureA);
        var serviceB = CreateBackupService(fixtureB);
        var backupA = await serviceA.CreateVerifiedAsync(
            fixtureA.DatabasePath,
            Path.Combine(fixtureA.BaseRoot, "backup"));
        var backupB = await serviceB.CreateVerifiedAsync(
            fixtureB.DatabasePath,
            Path.Combine(fixtureB.BaseRoot, "backup"));

        var wrongSource = await serviceA.VerifyForSourceAsync(
            backupA.ManifestPath,
            fixtureB.DatabasePath);
        Assert.False(wrongSource.IsValidForExpectedSource);
        Assert.Equal("SOURCE_IDENTITY_MISMATCH", wrongSource.FailureCode);

        File.Copy(backupB.BackupPath, backupA.BackupPath, overwrite: true);
        var swappedBackup = await serviceA.VerifyForSourceAsync(
            backupA.ManifestPath,
            fixtureA.DatabasePath);
        Assert.False(swappedBackup.IsValidForExpectedSource);
        Assert.Contains(
            swappedBackup.FailureCode,
            new[] { "BACKUP_SIZE_MISMATCH", "BACKUP_HASH_MISMATCH" });

        File.Copy(backupB.ManifestPath, backupA.ManifestPath, overwrite: true);
        File.Copy(backupB.ChecksumPath, backupA.ChecksumPath, overwrite: true);
        var swappedEvidence = await serviceA.VerifyForSourceAsync(
            backupA.ManifestPath,
            fixtureA.DatabasePath);
        Assert.False(swappedEvidence.IsValidForExpectedSource);
    }

    [Fact]
    public void Recovery_directory_and_filename_policy_are_frozen_and_outside_ordinary_retention()
    {
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BinTracker-RecoveryPreUpgrade");
        var name = new LineageMigrationBackupNameSource(
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 30, 3, 15, 0, 123, TimeSpan.Zero)),
            () => Guid.Parse("a1b2c3d4-0000-0000-0000-000000000000"))
            .CreateFileName(16);

        Assert.Equal(expected, LineageMigrationBackupPolicy.DefaultRecoveryDirectory);
        Assert.NotEqual(
            Path.GetFullPath(DatabaseConfiguration.DeveloperBackupFolder),
            Path.GetFullPath(LineageMigrationBackupPolicy.DefaultRecoveryDirectory));
        Assert.Matches(
            new Regex("^BinTracker-pre-lineage-v16-[0-9]{8}T[0-9]{9}Z-[a-f0-9]{8}\\.db$", RegexOptions.CultureInvariant),
            name);
        Assert.Equal(
            "BinTracker-pre-lineage-v16-20260830T031500123Z-a1b2c3d4.db",
            name);
    }

    [Theory]
    [InlineData(true, true, true, false, LineageMigrationRecoveryDisposition.PreserveActiveDatabase)]
    [InlineData(false, true, false, true, LineageMigrationRecoveryDisposition.PreserveActiveDatabase)]
    [InlineData(false, false, true, true, LineageMigrationRecoveryDisposition.ControlledRestoreEligible)]
    [InlineData(true, false, false, false, LineageMigrationRecoveryDisposition.RecoveryProhibited)]
    public void Recovery_classification_never_overwrites_a_valid_active_database(
        bool rolledBack,
        bool activeValid,
        bool backupVerified,
        bool committedOrPostflightFailed,
        LineageMigrationRecoveryDisposition expected)
    {
        var actual = LineageMigrationRecoveryClassifier.Classify(new(
            rolledBack,
            activeValid,
            backupVerified,
            committedOrPostflightFailed));

        Assert.Equal(expected, actual);
    }

    private static WindowsFileDatabaseUpgradeGate CreateGate(DatabaseFixture fixture) => new(
        Path.Combine(fixture.BaseRoot, "locks"),
        new NoConflictProbe());

    private static SqliteLineageMigrationBackupService CreateBackupService(DatabaseFixture fixture) => new(
        CreateGate(fixture),
        new SqliteLineageMigrationPreflight());

    private static Process StartChildRuntimeLease(string lockPath)
    {
        var escapedLockPath = lockPath.Replace("'", "''", StringComparison.Ordinal);
        var script =
            $"$s=[IO.File]::Open('{escapedLockPath}',[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read);" +
            "[Console]::Out.WriteLine('READY');[Console]::Out.Flush();" +
            "[Console]::In.ReadLine() | Out-Null;$s.Dispose()";
        var encodedScript = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(encodedScript);
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start child lease process.");
    }

    private static async Task ToggleFirstByteAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var value = stream.ReadByte();
        Assert.NotEqual(-1, value);
        stream.Position = 0;
        stream.WriteByte((byte)(value ^ 0xFF));
        await stream.FlushAsync();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(
        string fileName,
        string existingFileName,
        IntPtr securityAttributes);

    private sealed class NoConflictProbe : IDatabaseOperationConflictProbe
    {
        public void EnsureNoConflict(string databasePath)
        {
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class QueueNameSource(params string[] names) : ILineageMigrationBackupNameSource
    {
        private readonly Queue<string> names = new(names);

        public string CreateFileName(int sourceSchemaVersion)
        {
            Assert.NotEmpty(names);
            return names.Dequeue();
        }
    }

    private static async Task<string> HashAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream));
    }

    private sealed class ExpectedTestException : Exception;

    private sealed class DatabaseFixture : IDisposable
    {
        private DatabaseFixture(string baseRoot, string root, string databasePath)
        {
            BaseRoot = baseRoot;
            Root = root;
            DatabasePath = databasePath;
        }

        public string BaseRoot { get; }
        public string Root { get; }
        public string DatabasePath { get; }

        public static async Task<DatabaseFixture> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "BinTracker-LineageMigrationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var fixture = new DatabaseFixture(
                root,
                Path.Combine(root, "source"),
                Path.Combine(root, "source", "BinTracker.db"));
            Directory.CreateDirectory(fixture.Root);

            await using var db = fixture.CreateContext();
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            return fixture;
        }

        public async Task SeedLineageShapesAsync()
        {
            await using var db = CreateContext();
            db.Customers.Add(new Customer { Id = 1, Name = "Test customer" });
            var batch = new MovementBatch
            {
                Id = 1,
                MovementDate = new DateOnly(2026, 8, 20),
                MovementType = MovementType.In,
                Source = MovementSource.Batch
            };
            db.MovementBatches.Add(batch);
            var original = Movement(1, MovementSource.Batch, batch.Id);
            var batchPeer = Movement(2, MovementSource.Batch, batch.Id);
            db.BinMovements.AddRange(original, batchPeer);
            await db.SaveChangesAsync();

            var neutraliser = Movement(3, MovementSource.Batch);
            neutraliser.MovementType = MovementType.Out;
            neutraliser.ReversesMovementId = original.Id;
            var replacement = Movement(4, MovementSource.Batch);
            db.BinMovements.AddRange(neutraliser, replacement);
            await db.SaveChangesAsync();
            original.CorrectedByMovementId = neutraliser.Id;

            var operation = new MovementCorrectionOperation
            {
                Id = 1,
                ClientOperationId = Guid.NewGuid(),
                RequestFingerprint = new string('A', 64),
                Kind = MovementCorrectionKind.Single,
                Reason = "Correction test",
                ActorUserId = 1,
                ActorUsername = "tester",
                CreatedUtc = DateTime.UtcNow
            };
            db.MovementCorrectionOperations.Add(operation);
            await db.SaveChangesAsync();
            db.MovementCorrectionLines.Add(new MovementCorrectionLine
            {
                Id = 1,
                CorrectionOperationId = operation.Id,
                OriginalMovementId = original.Id,
                NeutralisingMovementId = neutraliser.Id,
                ReplacementMovementId = replacement.Id
            });

            var reversal = Movement(5, MovementSource.Batch);
            reversal.MovementType = MovementType.Out;
            reversal.ReversesMovementId = batchPeer.Id;
            db.BinMovements.Add(reversal);
            await db.SaveChangesAsync();
            batchPeer.CorrectedByMovementId = reversal.Id;

            var importRun = new ImportRun
            {
                Id = 1,
                SourceFileName = "test.xlsx",
                SourceClientPath = "test.xlsx",
                SourceSha256 = new string('B', 64),
                Status = "Completed",
                Username = "tester",
                SessionId = "test"
            };
            db.ImportRuns.Add(importRun);
            var imported = Movement(6, MovementSource.ExcelImport);
            imported.ImportRunId = importRun.Id;
            var adjustment = Movement(7, MovementSource.Adjustment);
            db.BinMovements.AddRange(imported, adjustment);
            await db.SaveChangesAsync();

            var secondNeutraliser = Movement(8, MovementSource.Batch);
            secondNeutraliser.MovementType = MovementType.Out;
            secondNeutraliser.ReversesMovementId = replacement.Id;
            var secondReplacement = Movement(9, MovementSource.Batch);
            db.BinMovements.AddRange(secondNeutraliser, secondReplacement);
            await db.SaveChangesAsync();
            replacement.CorrectedByMovementId = secondNeutraliser.Id;
            var secondOperation = new MovementCorrectionOperation
            {
                Id = 2,
                ClientOperationId = Guid.NewGuid(),
                RequestFingerprint = new string('C', 64),
                Kind = MovementCorrectionKind.Single,
                Reason = "Repeated correction test",
                ActorUserId = 1,
                ActorUsername = "tester",
                CreatedUtc = DateTime.UtcNow
            };
            db.MovementCorrectionOperations.Add(secondOperation);
            await db.SaveChangesAsync();
            db.MovementCorrectionLines.Add(new MovementCorrectionLine
            {
                Id = 2,
                CorrectionOperationId = secondOperation.Id,
                OriginalMovementId = replacement.Id,
                NeutralisingMovementId = secondNeutraliser.Id,
                ReplacementMovementId = secondReplacement.Id
            });
            await db.SaveChangesAsync();
        }

        public async Task ExecuteAsync(string sql)
        {
            await using var db = CreateContext();
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task SeedWholeBatchCorrectionAsync()
        {
            await using var db = CreateContext();
            db.Customers.Add(new Customer { Id = 1, Name = "Test customer" });
            var originalBatch = new MovementBatch
            {
                Id = 1,
                MovementDate = new DateOnly(2026, 8, 20),
                MovementType = MovementType.In,
                Source = MovementSource.Batch
            };
            var replacementBatch = new MovementBatch
            {
                Id = 2,
                MovementDate = new DateOnly(2026, 8, 21),
                MovementType = MovementType.In,
                Source = MovementSource.Batch
            };
            db.MovementBatches.AddRange(originalBatch, replacementBatch);
            var original = Movement(1, MovementSource.Batch, originalBatch.Id);
            db.BinMovements.Add(original);
            await db.SaveChangesAsync();
            var neutraliser = Movement(2, MovementSource.Batch);
            neutraliser.MovementType = MovementType.Out;
            neutraliser.ReversesMovementId = original.Id;
            var replacement = Movement(3, MovementSource.Batch, replacementBatch.Id);
            replacement.MovementDate = replacementBatch.MovementDate;
            db.BinMovements.AddRange(neutraliser, replacement);
            await db.SaveChangesAsync();
            original.CorrectedByMovementId = neutraliser.Id;
            var operation = new MovementCorrectionOperation
            {
                Id = 1,
                ClientOperationId = Guid.NewGuid(),
                RequestFingerprint = new string('D', 64),
                Kind = MovementCorrectionKind.WholeBatch,
                OriginalBatchId = originalBatch.Id,
                ReplacementBatchId = replacementBatch.Id,
                Reason = "Whole batch test",
                ActorUserId = 1,
                ActorUsername = "tester",
                CreatedUtc = DateTime.UtcNow
            };
            db.MovementCorrectionOperations.Add(operation);
            await db.SaveChangesAsync();
            db.MovementCorrectionLines.Add(new MovementCorrectionLine
            {
                Id = 1,
                CorrectionOperationId = operation.Id,
                OriginalMovementId = original.Id,
                NeutralisingMovementId = neutraliser.Id,
                ReplacementMovementId = replacement.Id
            });
            await db.SaveChangesAsync();
        }

        private BinTrackerDbContext CreateContext()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Pooling = false
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(connectionString)
                .Options;
            return new BinTrackerDbContext(options);
        }

        private static BinMovement Movement(long id, MovementSource source, int? batchId = null) => new()
        {
            Id = id,
            MovementDate = new DateOnly(2026, 8, 20),
            MovementType = MovementType.In,
            Source = source,
            CustomerId = 1,
            ContainerTypeId = 1,
            MovementBatchId = batchId,
            Quantity = 1,
            CreatedUtc = DateTime.UtcNow
        };

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(BaseRoot))
                Directory.Delete(BaseRoot, recursive: true);
        }
    }
}
