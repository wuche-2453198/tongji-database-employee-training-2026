using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Implementations;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.ModuleTests;

// Executes the real repositories and Dapper against recorded commands and synthetic rows.
// This checks command binding/mapping, not Oracle SQL execution, locking or transactions.
internal static class RepositoryContractTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Trainer repository binds filters pagination and maps rows", TrainerPagingAsync);
        yield return ("Course repository binds filters pagination and maps nullable rows", CoursePagingAsync);
        yield return ("Course repository binds expected states and observes affected rows", ConditionalUpdatesAsync);
        yield return ("Course repository capacity mapping and department gate", CapacityAndDepartmentAsync);
        yield return ("Course and trainer inserts bind output IDs", InsertIdsAsync);
    }

    private static async Task TrainerPagingAsync()
    {
        var db = new RecordedConnection();
        db.Rows.Columns.Add("TrainerId", typeof(decimal));
        db.Rows.Columns.Add("TrainerName", typeof(string));
        db.Rows.Columns.Add("StarLevel", typeof(decimal));
        db.Rows.Columns.Add("UpdatedAt", typeof(DateTime));
        db.Rows.Rows.Add(12m, "测试讲师", 4.5m, DBNull.Value);
        db.Scalar = 42m;
        var result = await new OracleTrainerRepository(db).GetAllAsync(new TrainerQuery
        {
            TrainerName = "x' OR 1=1 --", Company = "同济", IsInternal = "Y", Page = int.MaxValue, PageSize = 100
        }, CancellationToken.None);
        CheckPaging(db, 214748364600L);
        TestAssert.Equal("%x' OR 1=1 --%", db.Calls[1].Parameters["TrainerName"], "User text is a bind value.");
        TestAssert.True(!db.Calls[1].Sql.Contains("x' OR"), "User text must not enter SQL.");
        TestAssert.Equal(42L, result.Total, "Oracle numeric count maps to long.");
        TestAssert.Equal(12L, result.Items[0].TrainerId, "Oracle numeric ID maps to long.");
        TestAssert.Equal(4.5m, result.Items[0].StarLevel, "Fractional star level maps.");
        TestAssert.True(result.Items[0].UpdatedAt is null, "Nullable date maps.");
    }

    private static async Task CoursePagingAsync()
    {
        var db = new RecordedConnection { Scalar = 9m };
        db.Rows.Columns.Add("CourseId", typeof(decimal));
        db.Rows.Columns.Add("CourseName", typeof(string));
        db.Rows.Columns.Add("MaxStudents", typeof(decimal));
        db.Rows.Columns.Add("TrainerId", typeof(decimal));
        db.Rows.Columns.Add("StartAt", typeof(DateTime));
        db.Rows.Rows.Add(3m, "课程", 30m, DBNull.Value, DBNull.Value);
        var from = new DateTime(2026, 10, 1);
        var result = await new OracleCourseRepository(db).GetAllAsync(new CourseQuery
        {
            CourseName = "课程", CourseType = "技术培训", CourseStatus = "DRAFT",
            StartAtFrom = from, StartAtTo = from.AddDays(10), Page = 2, PageSize = 5
        }, CancellationToken.None);
        CheckPaging(db, 5L);
        TestAssert.Equal(from, db.Calls[1].Parameters["StartAtFrom"], "Date filter is bound.");
        TestAssert.Equal(30, result.Items[0].MaxStudents, "Numeric capacity maps to int.");
        TestAssert.True(result.Items[0].TrainerId is null && result.Items[0].StartAt is null, "Draft nullable fields map.");
    }

    private static void CheckPaging(RecordedConnection db, long offset)
    {
        TestAssert.Equal(2, db.Calls.Count, "Count and list execute separately.");
        var count = db.Calls[0];
        var page = db.Calls[1];
        TestAssert.True(count.Sql.Contains("COUNT(1)"), "First command counts.");
        TestAssert.True(page.Sql.Contains("OFFSET :Offset ROWS") && page.Sql.Contains("FETCH NEXT :PageSize ROWS ONLY"), "Real repository must page.");
        TestAssert.Equal(offset, page.Parameters["Offset"], "64-bit offset.");
        var countWhere = count.Sql[count.Sql.IndexOf("WHERE", StringComparison.Ordinal)..].Trim();
        TestAssert.True(page.Sql.Contains(countWhere), "Count and list share filters.");
        foreach (var parameter in count.Parameters)
            TestAssert.Equal(parameter.Value, page.Parameters[parameter.Key], "Filter binds agree.");
    }

    private static async Task ConditionalUpdatesAsync()
    {
        var db = new RecordedConnection { AffectedRows = 0 };
        var repository = new OracleCourseRepository(db);
        var updated = await repository.UpdateAsync(new TrainingCourse { CourseId = 7 }, "DRAFT", CancellationToken.None);
        TestAssert.True(!updated, "Zero-row course update reports failure.");
        TestAssert.True(db.Calls[0].Sql.Contains("AND COURSE_STATUS = :ExpectedStatus"), "PUT guards state in SQL.");
        TestAssert.Equal("DRAFT", db.Calls[0].Parameters["ExpectedStatus"], "PUT binds old state.");
        db.AffectedRows = 1;
        TestAssert.True(await repository.UpdateStatusAsync(7, "PUBLISHED", "CLOSED", CancellationToken.None), "One-row close succeeds.");
        TestAssert.Equal("PUBLISHED", db.Calls[1].Parameters["ExpectedStatus"], "Close guards published state.");
        TestAssert.Equal("CLOSED", db.Calls[1].Parameters["NewStatus"], "Close targets closed state.");
        TestAssert.True(!db.Calls[1].Sql.Contains("BUDGET_AMOUNT ="), "Close changes only state and update timestamp.");
        db.AffectedRows = 0;
        TestAssert.True(!await new OracleTrainerRepository(db).UpdateAsync(new Trainer { TrainerId = 5 }, CancellationToken.None), "Zero-row trainer PUT fails.");
    }

    private static async Task CapacityAndDepartmentAsync()
    {
        var db = new RecordedConnection();
        db.Rows.Columns.Add("MaxStudents", typeof(decimal));
        db.Rows.Columns.Add("ValidRegistrationCount", typeof(decimal));
        db.Rows.Rows.Add(20m, 7m);
        var repository = new OracleCourseRepository(db);
        var capacity = await repository.GetCapacityAsync(3, CancellationToken.None);
        TestAssert.Equal((20, 7), capacity!.Value, "Capacity maps through real Dapper.");
        foreach (var status in new[] { "REGISTERED", "SIGNED_IN", "COMPLETED", "ABSENT" })
            TestAssert.True(db.Calls[0].Sql.Contains("'" + status + "'"), "Capacity includes each active DDL status.");
        TestAssert.True(!db.Calls[0].Sql.Contains("'CANCELED'"), "Canceled rows are excluded.");
        db.Scalar = 0m;
        TestAssert.True(!await repository.DepartmentExistsAsync(999, CancellationToken.None), "Unknown department returns false.");
        TestAssert.True(db.Calls[1].Sql.StartsWith("SELECT"), "Department gate is read-only.");
        TestAssert.Equal(999L, db.Calls[1].Parameters["DeptId"], "Department ID is bound.");
    }

    private static async Task InsertIdsAsync()
    {
        var db = new RecordedConnection();
        TestAssert.Equal(123L, await new OracleTrainerRepository(db).CreateAsync(new Trainer(), CancellationToken.None), "Trainer output ID.");
        TestAssert.Equal(123L, await new OracleCourseRepository(db).CreateAsync(new TrainingCourse(), CancellationToken.None), "Course output ID.");
        TestAssert.True(db.Calls.All(c => c.Sql.Contains("RETURNING") && c.Parameters.Values.Any(v => Equals(v, 123L))), "Output ID binding is present.");
    }

    private sealed class RecordedConnection : DbConnection, IDbConnectionFactory
    {
        public List<(string Sql, Dictionary<string, object?> Parameters)> Calls { get; } = new();
        public DataTable Rows { get; } = new();
        public object Scalar { get; set; } = 0m;
        public int AffectedRows { get; set; } = 1;
        public bool IsConfigured => true;
        public Task<DbConnection> CreateOpenConnectionAsync(CancellationToken token) => Task.FromResult<DbConnection>(this);
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "recorded";
        public override string DataSource => "recorded";
        public override string ServerVersion => "test";
        public override ConnectionState State => ConnectionState.Open;
        public override void Open() { }
        public override void Close() { }
        public override void ChangeDatabase(string name) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolation) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new RecordedCommand(this);

        private sealed class RecordedCommand(RecordedConnection owner) : DbCommand
        {
            private readonly OracleCommand parameters = new();
            [AllowNull] public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; } = owner;
            protected override DbTransaction? DbTransaction { get; set; }
            protected override DbParameterCollection DbParameterCollection => parameters.Parameters;
            protected override DbParameter CreateDbParameter() => new OracleParameter();
            public override void Cancel() { }
            public override void Prepare() { }
            private void Record() => owner.Calls.Add((CommandText, parameters.Parameters.Cast<DbParameter>().ToDictionary(p => p.ParameterName, p => p.Value)));
            public override object ExecuteScalar() { Record(); return owner.Scalar; }
            public override int ExecuteNonQuery()
            {
                foreach (DbParameter p in parameters.Parameters)
                    if (p.Direction == ParameterDirection.Output) p.Value = 123L;
                Record();
                return owner.AffectedRows;
            }
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { Record(); return owner.Rows.CreateDataReader(); }
            protected override void Dispose(bool disposing) { if (disposing) parameters.Dispose(); base.Dispose(disposing); }
        }
    }
}
