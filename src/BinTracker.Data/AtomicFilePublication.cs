namespace BinTracker.Data;

internal enum AtomicFilePublicationMode
{
    CreateNew,
    CreateOrReplace,
    ReplaceExisting
}

/// <summary>
/// Publishes small Data-owned control files without exposing a partially written
/// target. The persistent sibling lease gives all BinTracker writers one stable
/// cross-process arbitration name; only the writer's unique temporary file is
/// eligible for cleanup.
/// </summary>
internal static class AtomicFilePublication
{
    internal static FileStream AcquireLease(string targetPath)
    {
        var fullTarget = Path.GetFullPath(targetPath);
        var directory = Path.GetDirectoryName(fullTarget)
            ?? throw new InvalidOperationException("Atomic publication folder is invalid.");
        Directory.CreateDirectory(directory);

        var leasePath = fullTarget + ".publish-lock";
        Exception? lastFailure = null;
        for (var attempt = 0; attempt < 200; attempt++)
        {
            try
            {
                return new FileStream(
                    leasePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.WriteThrough);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastFailure = ex;
                Thread.Sleep(10);
            }
        }

        throw new IOException(
            $"Could not obtain exclusive publication ownership for '{fullTarget}'.",
            lastFailure);
    }

    internal static void Write(
        string targetPath,
        Action<Stream> write,
        AtomicFilePublicationMode mode,
        FileStream? existingLease = null,
        Action? beforePublication = null)
    {
        ArgumentNullException.ThrowIfNull(write);

        var fullTarget = Path.GetFullPath(targetPath);
        var directory = Path.GetDirectoryName(fullTarget)
            ?? throw new InvalidOperationException("Atomic publication folder is invalid.");
        Directory.CreateDirectory(directory);

        var ownsLease = existingLease is null;
        var lease = existingLease ?? AcquireLease(fullTarget);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullTarget)}.{Guid.NewGuid():N}.tmp");
        Exception? failure = null;

        try
        {
            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                write(stream);
                stream.Flush(flushToDisk: true);
            }

            beforePublication?.Invoke();

            switch (mode)
            {
                case AtomicFilePublicationMode.CreateNew:
                    File.Move(temporaryPath, fullTarget, overwrite: false);
                    break;
                case AtomicFilePublicationMode.CreateOrReplace:
                    if (File.Exists(fullTarget))
                    {
                        File.Replace(temporaryPath, fullTarget, destinationBackupFileName: null,
                            ignoreMetadataErrors: false);
                    }
                    else
                    {
                        File.Move(temporaryPath, fullTarget, overwrite: false);
                    }
                    break;
                case AtomicFilePublicationMode.ReplaceExisting:
                    if (!File.Exists(fullTarget))
                        throw new FileNotFoundException(
                            "The file selected for atomic replacement no longer exists.", fullTarget);
                    File.Replace(temporaryPath, fullTarget, destinationBackupFileName: null,
                        ignoreMetadataErrors: false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch when (failure is not null)
            {
                // Preserve the publication failure. A collision-resistant orphan
                // is not authoritative and can be removed by later maintenance.
            }
            finally
            {
                if (ownsLease) lease.Dispose();
            }
        }
    }
}
