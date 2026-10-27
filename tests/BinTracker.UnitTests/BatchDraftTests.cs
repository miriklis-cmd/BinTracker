using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class BatchDraftTests
{
    [Fact]
    public void Draft_lines_survive_when_the_same_application_state_is_reused()
    {
        var state = new ApplicationState(new TestBusinessClock());
        state.DraftBatch.Lines.Add(Line(quantity: 20));

        var laterViewOfSameState = state.DraftBatch;

        Assert.Single(laterViewOfSameState.Lines);
        Assert.Equal(20, laterViewOfSameState.TotalQuantity);
    }

    [Fact]
    public void Clearing_draft_removes_unsaved_lines()
    {
        var state = new ApplicationState(new TestBusinessClock());
        state.DraftBatch.Lines.Add(Line(quantity: 20));

        state.DraftBatch.Clear();

        Assert.Empty(state.DraftBatch.Lines);
        Assert.False(state.DraftBatch.HasLines);
    }

    [Fact]
    public void Pending_in_is_reflected_in_preview_without_changing_database_balance()
    {
        var databaseBalance = 5;
        var preview = MovementPositionMath.Apply(databaseBalance, MovementType.In, 20);

        Assert.Equal(5, databaseBalance);
        Assert.Equal(-15, preview);
        Assert.Equal("15 CREDIT", MovementPositionMath.Format(preview));
    }

    [Fact]
    public void Pending_out_is_reflected_in_preview()
    {
        var preview = MovementPositionMath.Apply(5, MovementType.Out, 20);

        Assert.Equal(25, preview);
        Assert.Equal("25 OUT", MovementPositionMath.Format(preview));
    }

    [Fact]
    public void Owner_crash_restart_recovery_restores_identical_draft_and_prompt()
    {
        using var files = new DraftFiles();
        var owner = new MutableUserContext(41, "owner", "Owner", UserRole.Operator);
        var first = State(files, owner);
        first.DraftBatch.MovementDate = new DateOnly(2026, 8, 18);
        first.DraftBatch.MovementType = MovementType.Out;
        first.DraftBatch.Lines.Add(Line(quantity: 20, reference: "REF", notes: "Notes"));
        first.PersistDraft();

        var restored = State(files, owner);

        Assert.True(restored.RecoveryPromptPending);
        Assert.NotNull(restored.RecoveryDraftLastSavedAtUtc);
        Assert.Equal(new DateOnly(2026, 8, 18), restored.DraftBatch.MovementDate);
        Assert.Equal(MovementType.Out, restored.DraftBatch.MovementType);
        var line = Assert.Single(restored.DraftBatch.Lines);
        Assert.Equal(20, line.Quantity);
        Assert.Equal("REF", line.Reference);
        Assert.Equal("Notes", line.Notes);
    }

    [Fact]
    public void User_switch_hides_and_blocks_prior_owner_then_owner_recovers_same_draft()
    {
        using var files = new DraftFiles();
        var actor = new MutableUserContext(10, "user-a", "User A", UserRole.Operator);
        var state = State(files, actor);
        state.DraftBatch.Lines.Add(Line(quantity: 7, reference: "A-only"));
        state.PersistDraft();

        actor.Switch(20, "user-b", "User B", UserRole.Operator);

        Assert.False(state.DraftBatch.HasLines);
        Assert.False(state.RecoveryPromptPending);
        Assert.Throws<InvalidOperationException>(() =>
            state.CreateOwnedSaveRequest());
        state.DraftBatch.Lines.Add(Line(quantity: 3, reference: "B-only"));
        state.PersistDraft();
        state.ClearDraft();

        actor.Switch(10, "user-a", "User A", UserRole.Operator);

        var recovered = Assert.Single(state.DraftBatch.Lines);
        Assert.Equal(7, recovered.Quantity);
        Assert.Equal("A-only", recovered.Reference);
        Assert.Equal(10, state.CreateOwnedSaveRequest().DraftOwnerUserId);
        Assert.False(state.RecoveryPromptPending);
    }

    [Fact]
    public void Stable_user_id_not_username_or_display_name_controls_ownership()
    {
        using var files = new DraftFiles();
        var actor = new MutableUserContext(11, "shared", "Shared Name", UserRole.Operator);
        var state = State(files, actor);
        state.DraftBatch.Lines.Add(Line(quantity: 11));
        state.PersistDraft();

        actor.Switch(12, "shared", "Shared Name", UserRole.Operator);
        Assert.False(state.DraftBatch.HasLines);

        actor.Switch(11, "renamed", "Renamed Owner", UserRole.Operator);
        Assert.Equal(11, Assert.Single(state.DraftBatch.Lines).Quantity);
    }

    [Fact]
    public void Administrator_role_has_no_implicit_draft_takeover()
    {
        using var files = new DraftFiles();
        var actor = new MutableUserContext(21, "operator", "Operator", UserRole.Operator);
        var state = State(files, actor);
        state.DraftBatch.Lines.Add(Line(quantity: 21));
        state.PersistDraft();

        actor.Switch(22, "admin", "Administrator", UserRole.Administrator);

        Assert.False(state.DraftBatch.HasLines);
        Assert.Throws<InvalidOperationException>(() =>
            state.CreateOwnedSaveRequest());
        state.ClearDraft();
        actor.Switch(21, "operator", "Operator", UserRole.Operator);
        Assert.Equal(21, Assert.Single(state.DraftBatch.Lines).Quantity);
    }

    [Fact]
    public void Missing_authenticated_identity_detaches_and_denies_draft_operations()
    {
        using var files = new DraftFiles();
        var actor = new MutableUserContext(31, "owner", "Owner", UserRole.Operator);
        var state = State(files, actor);
        state.DraftBatch.Lines.Add(Line(quantity: 31));
        state.PersistDraft();

        actor.SignOut();

        Assert.Throws<InvalidOperationException>(() => state.DraftBatch);
        Assert.Throws<InvalidOperationException>(() => state.PersistDraft());
        Assert.Throws<InvalidOperationException>(() => state.ClearDraft());

        actor.Switch(31, "owner", "Owner", UserRole.Operator);
        Assert.Equal(31, Assert.Single(state.DraftBatch.Lines).Quantity);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("zero")]
    [InlineData("wrong")]
    [InlineData("duplicate")]
    public void Missing_invalid_foreign_or_ambiguous_ownership_fails_closed(string scenario)
    {
        using var files = new DraftFiles();
        var ownerPath = files.OwnerPath(44);
        Directory.CreateDirectory(files.DirectoryPath);
        var ownership = scenario switch
        {
            "missing" => string.Empty,
            "zero" => "\"OwnerUserId\": 0,",
            "wrong" => "\"OwnerUserId\": 45,",
            "duplicate" => "\"OwnerUserId\": 44, \"OwnerUserId\": 44,",
            _ => throw new InvalidOperationException()
        };
        File.WriteAllText(ownerPath, ValidJson(ownership));

        var result = files.Store.Load(44);

        Assert.Null(result.Draft);
        Assert.NotNull(result.Diagnostic);
        Assert.False(File.Exists(ownerPath));
        Assert.Single(Directory.GetFiles(files.DirectoryPath, Path.GetFileName(ownerPath) + ".corrupt-*"));
    }

    [Fact]
    public void Legacy_ownerless_draft_is_preserved_and_never_assigned()
    {
        using var files = new DraftFiles();
        Directory.CreateDirectory(files.DirectoryPath);
        File.WriteAllText(files.LegacyPath,
            """
            {"SchemaVersion":2,"MovementDate":"2026-08-18","MovementType":1,"Lines":[]}
            """);

        var result = files.Store.Load(51);

        Assert.Null(result.Draft);
        Assert.Contains("ownerless", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(files.LegacyPath));
        Assert.Single(Directory.GetFiles(files.DirectoryPath, "batch-entry-draft.json.corrupt-*"));
    }

    [Theory]
    [InlineData("null-lines")]
    [InlineData("null-line")]
    [InlineData("missing-field")]
    [InlineData("invalid-id")]
    [InlineData("invalid-enum")]
    [InlineData("wrong-type")]
    [InlineData("too-deep")]
    public void Malformed_nested_graphs_are_rejected_without_crashing(string scenario)
    {
        using var files = new DraftFiles();
        var path = files.OwnerPath(61);
        Directory.CreateDirectory(files.DirectoryPath);
        var line = """{"CustomerId":1,"CustomerCode":"ALBURY","CustomerName":"Albury","ContainerTypeId":1,"ContainerType":"Blue Bin","Quantity":20,"Reference":null,"Notes":null}""";
        var lines = scenario switch
        {
            "null-lines" => "null",
            "null-line" => "[null]",
            "missing-field" => "[{\"CustomerId\":1}]",
            "invalid-id" => "[{\"CustomerId\":0,\"CustomerCode\":\"ALBURY\",\"CustomerName\":\"Albury\",\"ContainerTypeId\":1,\"ContainerType\":\"Blue Bin\",\"Quantity\":20,\"Reference\":null,\"Notes\":null}]",
            "wrong-type" => "[{\"CustomerId\":\"one\",\"CustomerCode\":\"ALBURY\",\"CustomerName\":\"Albury\",\"ContainerTypeId\":1,\"ContainerType\":\"Blue Bin\",\"Quantity\":20,\"Reference\":null,\"Notes\":null}]",
            "too-deep" => "[[[[[[[[[[]]]]]]]]]]",
            _ => $"[{line}]"
        };
        var movementType = scenario == "invalid-enum" ? 99 : 1;
        File.WriteAllText(path, $$"""
            {"SchemaVersion":3,"OwnerUserId":61,"MovementDate":"2026-08-18","MovementType":{{movementType}},"Lines":{{lines}},"SavedAtUtc":"2026-08-18T00:00:00+00:00"}
            """);

        var result = files.Store.Load(61);

        Assert.Null(result.Draft);
        Assert.NotNull(result.Diagnostic);
        Assert.Single(Directory.GetFiles(files.DirectoryPath, Path.GetFileName(path) + ".corrupt-*"));
    }

    [Theory]
    [InlineData("invalid-revision")]
    [InlineData("wrong-revision-type")]
    [InlineData("invalid-state")]
    [InlineData("invalid-operation")]
    [InlineData("cleared-with-payload")]
    public void Malformed_schema4_revision_state_and_operation_identity_fail_closed(string scenario)
    {
        using var files = new DraftFiles();
        var path = files.OwnerPath(62);
        Directory.CreateDirectory(files.DirectoryPath);
        var revision = scenario switch
        {
            "invalid-revision" => "0",
            "wrong-revision-type" => "\"one\"",
            _ => "1"
        };
        var state = scenario == "invalid-state" ? "Unknown" :
            scenario == "cleared-with-payload" ? "Cleared" : "Active";
        var operationId = scenario == "invalid-operation"
            ? Guid.Empty
            : Guid.NewGuid();
        File.WriteAllText(path, $$"""
            {"SchemaVersion":4,"OwnerUserId":62,"Revision":{{revision}},"State":"{{state}}","ClientOperationId":"{{operationId:D}}","MovementDate":"2026-08-18","MovementType":1,"Lines":[{"CustomerId":1,"CustomerCode":"ALBURY","CustomerName":"Albury","ContainerTypeId":1,"ContainerType":"Blue Bin","Quantity":20,"Reference":null,"Notes":null}],"SavedAtUtc":"2026-08-18T00:00:00+00:00"}
            """);

        var result = files.Store.Load(62);

        Assert.Null(result.Draft);
        Assert.NotNull(result.Diagnostic);
        Assert.Single(Directory.GetFiles(files.DirectoryPath, Path.GetFileName(path) + ".corrupt-*"));
    }

    [Fact]
    public void Oversized_file_is_rejected_before_deserialization_and_preserved()
    {
        using var files = new DraftFiles();
        var path = files.OwnerPath(71);
        Directory.CreateDirectory(files.DirectoryPath);
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            stream.SetLength(FileBatchDraftStore.MaximumFileBytes + 1L);

        var result = files.Store.Load(71);

        Assert.Null(result.Draft);
        Assert.Contains("byte safety limit", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Single(Directory.GetFiles(files.DirectoryPath, Path.GetFileName(path) + ".corrupt-*"));
    }

    [Fact]
    public void Excessive_line_count_is_rejected_without_partial_recovery()
    {
        using var files = new DraftFiles();
        var path = files.OwnerPath(72);
        Directory.CreateDirectory(files.DirectoryPath);
        var line = """{"CustomerId":1,"CustomerCode":"A","CustomerName":"A","ContainerTypeId":1,"ContainerType":"B","Quantity":1,"Reference":null,"Notes":null}""";
        var lines = string.Join(',', Enumerable.Repeat(line, FileBatchDraftStore.MaximumLineCount + 1));
        File.WriteAllText(path, $$"""
            {"SchemaVersion":3,"OwnerUserId":72,"MovementDate":"2026-08-18","MovementType":1,"Lines":[{{lines}}],"SavedAtUtc":"2026-08-18T00:00:00+00:00"}
            """);

        var result = files.Store.Load(72);

        Assert.Null(result.Draft);
        Assert.Contains("line safety limit", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fixed_stale_temporary_name_cannot_collide_with_unique_publication()
    {
        using var files = new DraftFiles();
        var ownerPath = files.OwnerPath(81);
        Directory.CreateDirectory(files.DirectoryPath);
        var oldPredictableTemporaryPath = ownerPath + ".tmp";
        File.WriteAllText(oldPredictableTemporaryPath, "foreign temporary evidence");
        var state = State(files, new MutableUserContext(81, "owner", "Owner", UserRole.Operator));
        state.DraftBatch.Lines.Add(Line(quantity: 8));

        state.PersistDraft();

        Assert.Equal("foreign temporary evidence", File.ReadAllText(oldPredictableTemporaryPath));
        Assert.Equal(8, Assert.Single(files.Store.Load(81).Draft!.Lines).Quantity);
    }

    [Fact]
    public async Task Concurrent_unique_temporary_writers_publish_one_complete_revision()
    {
        using var files = new DraftFiles();
        var writes = Enumerable.Range(1, 12).Select(quantity => Task.Run(() =>
        {
            var draft = new DraftMovementBatch(new TestBusinessClock());
            draft.Lines.Add(Line(quantity: quantity));
            try
            {
                new FileBatchDraftStore(files.LegacyPath).Save(82, draft);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }));

        var results = await Task.WhenAll(writes);

        Assert.Single(results, succeeded => succeeded);
        var result = files.Store.Load(82);
        Assert.NotNull(result.Draft);
        Assert.InRange(Assert.Single(result.Draft.Lines).Quantity, 1, 12);
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public async Task Concurrent_save_and_clear_for_same_owner_accept_exactly_one_revision()
    {
        using var files = new DraftFiles();
        var owner = new MutableUserContext(83, "owner", "Owner", UserRole.Operator);
        var initial = State(files, owner);
        initial.DraftBatch.Lines.Add(Line(quantity: 5));
        initial.PersistDraft();

        var saver = State(files, owner);
        var clearer = State(files, owner);
        saver.DraftBatch.Lines[0] = Line(quantity: 8);
        _ = clearer.DraftBatch.Lines.Count;
        using var ready = new CountdownEvent(2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var save = AttemptAsync(ready, start.Task, saver.PersistDraft);
        var clear = AttemptAsync(ready, start.Task, clearer.ClearDraft);

        ready.Wait();
        start.SetResult();
        var outcomes = await Task.WhenAll(save, clear);

        Assert.Single(outcomes, outcome => outcome is null);
        Assert.Single(outcomes, outcome => outcome is InvalidOperationException);
        var persisted = files.Store.Load(83).Draft;
        if (persisted is not null)
            Assert.Equal(8, Assert.Single(persisted.Lines).Quantity);
    }

    [Fact]
    public void Stale_save_after_accepted_clear_cannot_resurrect_draft()
    {
        using var files = new DraftFiles();
        var owner = new MutableUserContext(84, "owner", "Owner", UserRole.Operator);
        var initial = State(files, owner);
        initial.DraftBatch.Lines.Add(Line(quantity: 5));
        initial.PersistDraft();
        var staleSaver = State(files, owner);
        var clearer = State(files, owner);
        _ = staleSaver.DraftBatch.Lines.Count;
        _ = clearer.DraftBatch.Lines.Count;

        clearer.ClearDraft();
        var failure = Assert.Throws<InvalidOperationException>(staleSaver.PersistDraft);

        Assert.Contains("changed in another operation", failure.Message, StringComparison.Ordinal);
        Assert.Null(files.Store.Load(84).Draft);
    }

    [Fact]
    public void Stale_clear_cannot_delete_newer_legitimate_save()
    {
        using var files = new DraftFiles();
        var owner = new MutableUserContext(85, "owner", "Owner", UserRole.Operator);
        var initial = State(files, owner);
        initial.DraftBatch.Lines.Add(Line(quantity: 5));
        initial.PersistDraft();
        var staleClearer = State(files, owner);
        var newerSaver = State(files, owner);
        _ = staleClearer.DraftBatch.Lines.Count;
        newerSaver.DraftBatch.Lines[0] = Line(quantity: 9);

        newerSaver.PersistDraft();
        Assert.Throws<InvalidOperationException>(staleClearer.ClearDraft);

        Assert.Equal(9, Assert.Single(files.Store.Load(85).Draft!.Lines).Quantity);
    }

    [Fact]
    public async Task Concurrent_different_owner_operations_remain_independent()
    {
        using var files = new DraftFiles();
        var first = State(files, new MutableUserContext(86, "first", "First", UserRole.Operator));
        var second = State(files, new MutableUserContext(87, "second", "Second", UserRole.Operator));
        first.DraftBatch.Lines.Add(Line(quantity: 6));
        second.DraftBatch.Lines.Add(Line(quantity: 7));
        using var ready = new CountdownEvent(2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var firstSave = AttemptAsync(ready, start.Task, first.PersistDraft);
        var secondSave = AttemptAsync(ready, start.Task, second.PersistDraft);
        ready.Wait();
        start.SetResult();
        var outcomes = await Task.WhenAll(firstSave, secondSave);

        Assert.All(outcomes, Assert.Null);
        Assert.Equal(6, Assert.Single(files.Store.Load(86).Draft!.Lines).Quantity);
        Assert.Equal(7, Assert.Single(files.Store.Load(87).Draft!.Lines).Quantity);
    }

    [Fact]
    public void Schema3_recovery_is_upgraded_with_durable_operation_identity_before_execution()
    {
        using var files = new DraftFiles();
        Directory.CreateDirectory(files.DirectoryPath);
        File.WriteAllText(files.OwnerPath(88), ValidJson("\"OwnerUserId\": 88,"));
        var owner = new MutableUserContext(88, "owner", "Owner", UserRole.Operator);
        var restored = State(files, owner);

        var request = restored.CreateOwnedSaveRequest();
        var restarted = State(files, owner);
        var retry = restarted.CreateOwnedSaveRequest();

        Assert.NotEqual(Guid.Empty, request.ClientOperationId);
        Assert.Equal(request.ClientOperationId, retry.ClientOperationId);
        Assert.Contains("\"SchemaVersion\": 4", File.ReadAllText(files.OwnerPath(88)));
    }

    [Fact]
    public void Quarantine_uses_no_overwrite_and_preserves_existing_evidence()
    {
        using var files = new DraftFiles();
        var path = files.OwnerPath(91);
        Directory.CreateDirectory(files.DirectoryPath);
        File.WriteAllText(path, "not-json");
        var existing = path + ".corrupt-existing";
        File.WriteAllText(existing, "existing evidence");

        var result = files.Store.Load(91);

        Assert.Null(result.Draft);
        Assert.Equal("existing evidence", File.ReadAllText(existing));
        Assert.Single(Directory.GetFiles(files.DirectoryPath, Path.GetFileName(path) + ".corrupt-*"),
            candidate => !candidate.EndsWith("corrupt-existing", StringComparison.Ordinal));
    }

    [Fact]
    public void Quarantine_failure_reports_truthfully_and_leaves_original_evidence()
    {
        using var files = new DraftFiles();
        var path = files.OwnerPath(92);
        Directory.CreateDirectory(files.DirectoryPath);
        File.WriteAllText(path, "not-json");

        using var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var result = files.Store.Load(92);

        Assert.Null(result.Draft);
        Assert.Contains("Quarantine failed", result.Diagnostic, StringComparison.Ordinal);
        Assert.Contains("remains in place", result.Diagnostic, StringComparison.Ordinal);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Failed_atomic_publication_preserves_previous_recoverable_draft()
    {
        using var files = new DraftFiles();
        var owner = new MutableUserContext(93, "owner", "Owner", UserRole.Operator);
        var original = State(files, owner);
        original.DraftBatch.Lines.Add(Line(quantity: 5));
        original.PersistDraft();
        var replacement = State(files, owner);
        replacement.DraftBatch.Lines[0] = Line(quantity: 9);
        var path = files.OwnerPath(93);

        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var failure = Record.Exception(replacement.PersistDraft);
            Assert.True(failure is IOException or UnauthorizedAccessException);
        }

        Assert.Equal(5, Assert.Single(files.Store.Load(93).Draft!.Lines).Quantity);
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, Path.GetFileName(path) + ".*.tmp"));
    }

    [Fact]
    public void Failed_discard_preserves_owned_in_memory_and_persisted_draft()
    {
        using var files = new DraftFiles();
        var state = State(files, new MutableUserContext(94, "owner", "Owner", UserRole.Operator));
        state.DraftBatch.Lines.Add(Line(quantity: 6));
        state.PersistDraft();
        var path = files.OwnerPath(94);

        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var failure = Record.Exception(state.ClearDraft);
            Assert.True(failure is IOException or UnauthorizedAccessException);
            Assert.Equal(6, Assert.Single(state.DraftBatch.Lines).Quantity);
        }

        Assert.Equal(6, Assert.Single(files.Store.Load(94).Draft!.Lines).Quantity);
    }

    [Fact]
    public void Clear_removes_only_authenticated_owners_file()
    {
        using var files = new DraftFiles();
        var first = new DraftMovementBatch(new TestBusinessClock());
        first.Lines.Add(Line(quantity: 1));
        files.Store.Save(101, first);
        var second = new DraftMovementBatch(new TestBusinessClock());
        second.Lines.Add(Line(quantity: 2));
        files.Store.Save(102, second);

        var secondRevision = files.Store.Load(102).StorageRevision;
        files.Store.Clear(102, secondRevision, second.ClientOperationId);

        Assert.NotNull(files.Store.Load(101).Draft);
        Assert.Null(files.Store.Load(102).Draft);
    }

    [Fact]
    public void Clearing_persisted_draft_publishes_non_recoverable_tombstone()
    {
        using var files = new DraftFiles();
        var state = State(files, new MutableUserContext(111, "owner", "Owner", UserRole.Operator));
        state.DraftBatch.Lines.Add(Line(quantity: 20));
        state.PersistDraft();
        Assert.True(File.Exists(files.OwnerPath(111)));

        state.ClearDraft();

        Assert.True(File.Exists(files.OwnerPath(111)));
        Assert.Contains("\"State\": \"Cleared\"", File.ReadAllText(files.OwnerPath(111)));
        Assert.Null(files.Store.Load(111).Draft);
        Assert.False(state.DraftBatch.HasLines);
    }

    [Fact]
    public void Recovery_prompt_can_be_handled_without_discarding_owned_draft()
    {
        using var files = new DraftFiles();
        var owner = new MutableUserContext(121, "owner", "Owner", UserRole.Operator);
        var first = State(files, owner);
        first.DraftBatch.Lines.Add(Line(quantity: 3));
        first.PersistDraft();
        var restored = State(files, owner);

        Assert.True(restored.RecoveryPromptPending);
        restored.MarkRecoveryPromptHandled();

        Assert.False(restored.RecoveryPromptPending);
        Assert.True(restored.DraftBatch.HasLines);
    }

    [Fact]
    public void Draft_created_in_current_process_does_not_set_recovery_prompt_pending()
    {
        using var files = new DraftFiles();
        var state = State(files, new MutableUserContext(131, "owner", "Owner", UserRole.Operator));
        state.DraftBatch.Lines.Add(Line(quantity: 3));
        state.PersistDraft();

        Assert.False(state.RecoveryPromptPending);
    }

    private static ApplicationState State(DraftFiles files, IUserContext owner) =>
        new(files.Store, new TestBusinessClock(), owner);

    private static async Task<Exception?> AttemptAsync(
        CountdownEvent ready,
        Task start,
        Action action)
    {
        ready.Signal();
        await start;
        return Record.Exception(action);
    }

    private static DraftMovementLine Line(
        int quantity,
        string? reference = null,
        string? notes = null) =>
        new(1, "ALBURY", "Albury", 1, "Blue Bin", quantity, reference, notes);

    private static string ValidJson(string ownership) => $$"""
        {"SchemaVersion":3,{{ownership}}"MovementDate":"2026-08-18","MovementType":1,"Lines":[{"CustomerId":1,"CustomerCode":"ALBURY","CustomerName":"Albury","ContainerTypeId":1,"ContainerType":"Blue Bin","Quantity":20,"Reference":null,"Notes":null}],"SavedAtUtc":"2026-08-18T00:00:00+00:00"}
        """;

    private sealed class DraftFiles : IDisposable
    {
        public DraftFiles()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"BinTrackerTests-{Guid.NewGuid():N}");
            LegacyPath = Path.Combine(DirectoryPath, "batch-entry-draft.json");
            Store = new FileBatchDraftStore(LegacyPath);
        }

        public string DirectoryPath { get; }
        public string LegacyPath { get; }
        public FileBatchDraftStore Store { get; }

        public string OwnerPath(int ownerUserId) =>
            Path.Combine(DirectoryPath, $"batch-entry-draft.user-{ownerUserId}.json");

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private sealed class MutableUserContext(
        int initialUserId,
        string initialUsername,
        string initialDisplayName,
        UserRole initialRole) : IUserContext
    {
        public string SessionId { get; } = Guid.NewGuid().ToString("N");
        public int? UserId { get; private set; } = initialUserId;
        public string Username { get; private set; } = initialUsername;
        public string DisplayName { get; private set; } = initialDisplayName;
        public UserRole Role { get; private set; } = initialRole;
        public bool MustChangePassword => false;
        public bool IsAuthenticated => UserId.HasValue;

        public void Switch(int userId, string username, string displayName, UserRole role)
        {
            UserId = userId;
            Username = username;
            DisplayName = displayName;
            Role = role;
        }

        public void SignOut()
        {
            UserId = null;
            Username = "anonymous";
            DisplayName = "Not signed in";
            Role = UserRole.Viewer;
        }
    }
}
