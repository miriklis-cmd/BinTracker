namespace BinTracker.Data;

// One provider DDL definition serves migration and structural capability comparison.
internal static class SqliteLineageSchema17Definition
{
    internal const string LogicalTables = """
            CREATE TABLE LogicalMovementBatches (
                Id INTEGER NOT NULL CONSTRAINT PK_LogicalMovementBatches PRIMARY KEY AUTOINCREMENT,
                RootMovementBatchId INTEGER NULL,
                Status INTEGER NOT NULL CONSTRAINT CK_LogicalMovementBatches_Status CHECK (Status IN (0,1,2,3)),
                CurrentGenerationNumber INTEGER NULL CONSTRAINT CK_LogicalMovementBatches_CurrentGeneration CHECK (CurrentGenerationNumber IS NULL OR CurrentGenerationNumber >= 0),
                LineCount INTEGER NOT NULL CONSTRAINT CK_LogicalMovementBatches_LineCount CHECK (LineCount > 0),
                StatusReasonCode TEXT NULL,
                CreatedUtc TEXT NOT NULL,
                CONSTRAINT FK_LogicalMovementBatches_MovementBatches_RootMovementBatchId FOREIGN KEY (RootMovementBatchId) REFERENCES MovementBatches (Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IX_LogicalMovementBatches_RootMovementBatchId ON LogicalMovementBatches (RootMovementBatchId) WHERE RootMovementBatchId IS NOT NULL;
            CREATE INDEX IX_LogicalMovementBatches_Status_CurrentGeneration ON LogicalMovementBatches (Status, CurrentGenerationNumber);

            CREATE TABLE LogicalMovementLines (
                Id INTEGER NOT NULL CONSTRAINT PK_LogicalMovementLines PRIMARY KEY AUTOINCREMENT,
                LogicalMovementBatchId INTEGER NOT NULL,
                RootMovementId INTEGER NOT NULL,
                OriginalDisplayOrdinal INTEGER NOT NULL CONSTRAINT CK_LogicalMovementLines_Ordinal CHECK (OriginalDisplayOrdinal >= 0),
                CreatedUtc TEXT NOT NULL,
                CONSTRAINT FK_LogicalMovementLines_LogicalMovementBatches_Root FOREIGN KEY (LogicalMovementBatchId) REFERENCES LogicalMovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementLines_BinMovements_RootMovement FOREIGN KEY (RootMovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT,
                CONSTRAINT UQ_LogicalMovementLines_Root_Id UNIQUE (LogicalMovementBatchId, Id)
            );
            CREATE UNIQUE INDEX IX_LogicalMovementLines_RootMovementId ON LogicalMovementLines (RootMovementId);
            CREATE UNIQUE INDEX IX_LogicalMovementLines_Root_Ordinal ON LogicalMovementLines (LogicalMovementBatchId, OriginalDisplayOrdinal);

            CREATE TABLE LogicalMovementGenerations (
                Id INTEGER NOT NULL CONSTRAINT PK_LogicalMovementGenerations PRIMARY KEY AUTOINCREMENT,
                LogicalMovementBatchId INTEGER NOT NULL,
                GenerationNumber INTEGER NOT NULL CONSTRAINT CK_LogicalMovementGenerations_Number CHECK (GenerationNumber >= 0),
                PreviousGenerationNumber INTEGER NULL,
                MovementCorrectionOperationId INTEGER NULL,
                Kind INTEGER NOT NULL CONSTRAINT CK_LogicalMovementGenerations_Kind CHECK (Kind IN (0,1,2,3,4,5,6,7)),
                LineCount INTEGER NOT NULL CONSTRAINT CK_LogicalMovementGenerations_LineCount CHECK (LineCount > 0),
                CreatedUtc TEXT NOT NULL,
                CONSTRAINT FK_LogicalMovementGenerations_Root FOREIGN KEY (LogicalMovementBatchId) REFERENCES LogicalMovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerations_Operation FOREIGN KEY (MovementCorrectionOperationId) REFERENCES MovementCorrectionOperations (Id) ON DELETE RESTRICT,
                CONSTRAINT CK_LogicalMovementGenerations_Predecessor CHECK ((GenerationNumber=0 AND PreviousGenerationNumber IS NULL) OR (GenerationNumber>0 AND PreviousGenerationNumber=GenerationNumber-1)),
                CONSTRAINT UQ_LogicalMovementGenerations_Root_Id UNIQUE (LogicalMovementBatchId, Id),
                CONSTRAINT UQ_LogicalMovementGenerations_Root_Number UNIQUE (LogicalMovementBatchId, GenerationNumber)
            );
            CREATE UNIQUE INDEX IX_LogicalMovementGenerations_Operation ON LogicalMovementGenerations (MovementCorrectionOperationId) WHERE MovementCorrectionOperationId IS NOT NULL;

            CREATE TABLE LogicalMovementGenerationLines (
                Id INTEGER NOT NULL CONSTRAINT PK_LogicalMovementGenerationLines PRIMARY KEY AUTOINCREMENT,
                LogicalMovementBatchId INTEGER NOT NULL,
                LogicalMovementGenerationId INTEGER NOT NULL,
                LogicalMovementLineId INTEGER NOT NULL,
                State INTEGER NOT NULL CONSTRAINT CK_LogicalMovementGenerationLines_State CHECK (State IN (0,1)),
                Action INTEGER NOT NULL CONSTRAINT CK_LogicalMovementGenerationLines_Action CHECK (Action IN (0,1,2,3,4,5,6,7)),
                AppliedFieldMask INTEGER NOT NULL CONSTRAINT CK_LogicalMovementGenerationLines_FieldMask CHECK (AppliedFieldMask BETWEEN 0 AND 127),
                PreviousGenerationLineId INTEGER NULL,
                ResultEffectiveMovementId INTEGER NULL,
                LastEffectiveMovementId INTEGER NULL,
                TerminalReversalMovementId INTEGER NULL,
                CreatedUtc TEXT NOT NULL,
                CONSTRAINT FK_LogicalMovementGenerationLines_Root FOREIGN KEY (LogicalMovementBatchId) REFERENCES LogicalMovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerationLines_Generation FOREIGN KEY (LogicalMovementBatchId, LogicalMovementGenerationId) REFERENCES LogicalMovementGenerations (LogicalMovementBatchId, Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerationLines_Line FOREIGN KEY (LogicalMovementBatchId, LogicalMovementLineId) REFERENCES LogicalMovementLines (LogicalMovementBatchId, Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerationLines_Previous FOREIGN KEY (PreviousGenerationLineId) REFERENCES LogicalMovementGenerationLines (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerationLines_Result FOREIGN KEY (ResultEffectiveMovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerationLines_Last FOREIGN KEY (LastEffectiveMovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementGenerationLines_Reversal FOREIGN KEY (TerminalReversalMovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT,
                CONSTRAINT CK_LogicalMovementGenerationLines_Pointers CHECK (
                    (State=0 AND ResultEffectiveMovementId IS NOT NULL AND LastEffectiveMovementId IS NULL AND TerminalReversalMovementId IS NULL) OR
                    (State=1 AND ResultEffectiveMovementId IS NULL AND LastEffectiveMovementId IS NOT NULL AND TerminalReversalMovementId IS NOT NULL)),
                CONSTRAINT UQ_LogicalMovementGenerationLines_Generation_Line UNIQUE (LogicalMovementGenerationId, LogicalMovementLineId),
                CONSTRAINT UQ_LogicalMovementGenerationLines_Root_Id UNIQUE (LogicalMovementBatchId, Id)
            );
            CREATE INDEX IX_LogicalMovementGenerationLines_Current ON LogicalMovementGenerationLines (LogicalMovementBatchId, LogicalMovementGenerationId);
            CREATE INDEX IX_LogicalMovementGenerationLines_Result ON LogicalMovementGenerationLines (ResultEffectiveMovementId);
            CREATE INDEX IX_LogicalMovementGenerationLines_Last ON LogicalMovementGenerationLines (LastEffectiveMovementId);
            CREATE INDEX IX_LogicalMovementGenerationLines_Reversal ON LogicalMovementGenerationLines (TerminalReversalMovementId);

            CREATE TABLE LogicalMovementLedgerLinks (
                BinMovementId INTEGER NOT NULL CONSTRAINT PK_LogicalMovementLedgerLinks PRIMARY KEY,
                LogicalMovementBatchId INTEGER NOT NULL,
                LogicalMovementLineId INTEGER NOT NULL,
                Role INTEGER NOT NULL CONSTRAINT CK_LogicalMovementLedgerLinks_Role CHECK (Role IN (0,1,2,3,4)),
                IntroducedByGenerationLineId INTEGER NULL,
                LegacyMovementCorrectionLineId INTEGER NULL,
                CreatedUtc TEXT NOT NULL,
                CONSTRAINT FK_LogicalMovementLedgerLinks_Movement FOREIGN KEY (BinMovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementLedgerLinks_Line FOREIGN KEY (LogicalMovementBatchId, LogicalMovementLineId) REFERENCES LogicalMovementLines (LogicalMovementBatchId, Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementLedgerLinks_Introduced FOREIGN KEY (LogicalMovementBatchId, IntroducedByGenerationLineId) REFERENCES LogicalMovementGenerationLines (LogicalMovementBatchId, Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementLedgerLinks_LegacyLine FOREIGN KEY (LegacyMovementCorrectionLineId) REFERENCES MovementCorrectionLines (Id) ON DELETE RESTRICT
            );
            CREATE INDEX IX_LogicalMovementLedgerLinks_Root_Line ON LogicalMovementLedgerLinks (LogicalMovementBatchId, LogicalMovementLineId);
            CREATE UNIQUE INDEX IX_LogicalMovementLedgerLinks_LegacyLine_Role ON LogicalMovementLedgerLinks (LegacyMovementCorrectionLineId, Role) WHERE LegacyMovementCorrectionLineId IS NOT NULL;

            CREATE TABLE LogicalMovementPhysicalOutputs (
                MovementBatchId INTEGER NOT NULL CONSTRAINT PK_LogicalMovementPhysicalOutputs PRIMARY KEY,
                LogicalMovementBatchId INTEGER NOT NULL,
                LogicalMovementGenerationId INTEGER NULL,
                LegacyMovementCorrectionOperationId INTEGER NULL,
                CreatedUtc TEXT NOT NULL,
                CONSTRAINT FK_LogicalMovementPhysicalOutputs_Batch FOREIGN KEY (MovementBatchId) REFERENCES MovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementPhysicalOutputs_Root FOREIGN KEY (LogicalMovementBatchId) REFERENCES LogicalMovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementPhysicalOutputs_Generation FOREIGN KEY (LogicalMovementBatchId, LogicalMovementGenerationId) REFERENCES LogicalMovementGenerations (LogicalMovementBatchId, Id) ON DELETE RESTRICT,
                CONSTRAINT FK_LogicalMovementPhysicalOutputs_LegacyOperation FOREIGN KEY (LegacyMovementCorrectionOperationId) REFERENCES MovementCorrectionOperations (Id) ON DELETE RESTRICT,
                CONSTRAINT CK_LogicalMovementPhysicalOutputs_Selector CHECK ((LogicalMovementGenerationId IS NULL) <> (LegacyMovementCorrectionOperationId IS NULL))
            );
            CREATE UNIQUE INDEX IX_LogicalMovementPhysicalOutputs_Generation ON LogicalMovementPhysicalOutputs (LogicalMovementGenerationId) WHERE LogicalMovementGenerationId IS NOT NULL;
            CREATE UNIQUE INDEX IX_LogicalMovementPhysicalOutputs_LegacyOperation ON LogicalMovementPhysicalOutputs (LegacyMovementCorrectionOperationId) WHERE LegacyMovementCorrectionOperationId IS NOT NULL;

            CREATE TABLE SingleMovementResponseReceipts (
                ClientOperationId TEXT NOT NULL CONSTRAINT PK_SingleMovementResponseReceipts PRIMARY KEY,
                MovementId INTEGER NOT NULL,
                BusinessDate TEXT NOT NULL,
                ResultingPosition INTEGER NOT NULL CONSTRAINT CK_SingleMovementResponseReceipts_ResultingPosition CHECK (ResultingPosition BETWEEN -2147483648 AND 2147483647),
                CONSTRAINT FK_SingleMovementResponseReceipts_BinMovements_MovementId FOREIGN KEY (MovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IX_SingleMovementResponseReceipts_MovementId ON SingleMovementResponseReceipts (MovementId);
            """;

    internal const string CorrectionOperations = """
            CREATE TABLE MovementCorrectionOperations (
                Id INTEGER NOT NULL CONSTRAINT PK_MovementCorrectionOperations PRIMARY KEY AUTOINCREMENT,
                ClientOperationId TEXT NOT NULL,
                RequestFingerprint TEXT NOT NULL,
                Kind INTEGER NOT NULL CONSTRAINT CK_MovementCorrectionOperations_Kind CHECK (Kind IN (0,1,2,3)),
                OriginalBatchId INTEGER NULL,
                ReplacementBatchId INTEGER NULL,
                Reason TEXT NOT NULL,
                ActorUserId INTEGER NOT NULL,
                ActorUsername TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL,
                RequestJson TEXT NULL,
                RequestSchemaVersion INTEGER NULL CONSTRAINT CK_MovementCorrectionOperations_RequestSchemaVersion CHECK (RequestSchemaVersion IS NULL OR RequestSchemaVersion > 0),
                LogicalMovementBatchId INTEGER NULL,
                ExpectedGenerationNumber INTEGER NULL CONSTRAINT CK_MovementCorrectionOperations_ExpectedGeneration CHECK (ExpectedGenerationNumber IS NULL OR ExpectedGenerationNumber >= 0),
                ResultGenerationNumber INTEGER NULL CONSTRAINT CK_MovementCorrectionOperations_ResultGeneration CHECK (ResultGenerationNumber IS NULL OR ResultGenerationNumber >= 0),
                CONSTRAINT FK_MovementCorrectionOperations_MovementBatches_OriginalBatchId FOREIGN KEY (OriginalBatchId) REFERENCES MovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_MovementCorrectionOperations_MovementBatches_ReplacementBatchId FOREIGN KEY (ReplacementBatchId) REFERENCES MovementBatches (Id) ON DELETE RESTRICT,
                CONSTRAINT FK_MovementCorrectionOperations_LogicalMovementBatches_LogicalMovementBatchId FOREIGN KEY (LogicalMovementBatchId) REFERENCES LogicalMovementBatches (Id) ON DELETE RESTRICT
            );
            """;
    internal const string CorrectionOperationIndexes = """
            CREATE UNIQUE INDEX IX_MovementCorrectionOperations_ClientOperationId ON MovementCorrectionOperations (ClientOperationId);
            CREATE UNIQUE INDEX IX_MovementCorrectionOperations_ResultGeneration
                ON MovementCorrectionOperations (LogicalMovementBatchId, ResultGenerationNumber)
                WHERE ResultGenerationNumber IS NOT NULL;
            CREATE INDEX IX_MovementCorrectionOperations_LogicalMovementBatchId ON MovementCorrectionOperations (LogicalMovementBatchId);
            """;

    internal const string AuditOperationColumn = "MovementCorrectionOperationId INTEGER NULL REFERENCES MovementCorrectionOperations(Id) ON DELETE RESTRICT";
    internal const string AuditOperationIndex = "CREATE UNIQUE INDEX IX_AuditEvents_MovementCorrectionOperationId ON AuditEvents (MovementCorrectionOperationId) WHERE MovementCorrectionOperationId IS NOT NULL;";
}
