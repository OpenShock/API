namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Reusable raw SQL for migrations.
/// </summary>
public static class MigrationQueries
{
    /// <summary>
    /// SQL that aborts the migration with a clear error if any existing value in <paramref name="column"/> is longer
    /// than <paramref name="maxLength"/> characters. Run it before narrowing a column whose values must not be
    /// truncated, like credentials. Table and column names are inlined into the SQL, only pass constants.
    /// </summary>
    public static string FailIfLonger(string table, string column, int maxLength) =>
        $"""
         DO $$
         DECLARE offending bigint;
         BEGIN
             SELECT count(*) INTO offending FROM {table} WHERE char_length({column}) > {maxLength};
             IF offending > 0 THEN
                 RAISE EXCEPTION '% row(s) in {table}.{column} exceed the new limit of {maxLength} characters. Fix or remove them manually before re-running this migration.', offending;
             END IF;
         END $$;
         """;
}
