using System.Security.Claims;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Dtos.Registration;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.ModuleTests;

internal sealed class FakeCourseRepository : ICourseRepository
{
    public TrainingCourse? Course { get; set; }

    public bool UpdateResult { get; set; } = true;

    public bool StatusUpdateResult { get; set; } = true;

    public long CreatedId { get; set; } = 100;

    public bool DepartmentExistsResult { get; set; } = true;

    public Task<bool> DepartmentExistsAsync(long deptId, CancellationToken cancellationToken)
        => Task.FromResult(DepartmentExistsResult);

    public (int MaxStudents, int ValidRegistrationCount)? Capacity { get; set; }

    public TrainingCourse? LastUpdatedCourse { get; private set; }

    public string? LastUpdateExpectedStatus { get; private set; }

    public string? LastStatusExpectedStatus { get; private set; }

    public string? LastStatusNewStatus { get; private set; }

    public int UpdateCallCount { get; private set; }

    public Task<(IReadOnlyList<TrainingCourse> Items, long Total)> GetAllAsync(
        CourseQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TrainingCourse> items = Course is null
            ? Array.Empty<TrainingCourse>()
            : new[] { Course };

        return Task.FromResult((items, (long)items.Count));
    }

    public Task<TrainingCourse?> GetByIdAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Course?.CourseId == courseId
                ? Course
                : null);
    }

    public Task<long> CreateAsync(
        TrainingCourse course,
        CancellationToken cancellationToken)
    {
        Course = CopyCourse(
            course,
            CreatedId,
            course.CourseStatus);

        return Task.FromResult(CreatedId);
    }

    public Task<bool> UpdateAsync(
        TrainingCourse course,
        string expectedStatus,
        CancellationToken cancellationToken)
    {
        UpdateCallCount++;
        LastUpdatedCourse = course;
        LastUpdateExpectedStatus = expectedStatus;

        if (UpdateResult)
        {
            Course = CopyCourse(
                course,
                course.CourseId,
                expectedStatus);
        }

        return Task.FromResult(UpdateResult);
    }

    public Task<(int MaxStudents, int ValidRegistrationCount)?> GetCapacityAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Capacity);
    }

    public Task<bool> UpdateStatusAsync(
        long courseId,
        string expectedStatus,
        string newStatus,
        CancellationToken cancellationToken)
    {
        LastStatusExpectedStatus = expectedStatus;
        LastStatusNewStatus = newStatus;

        if (StatusUpdateResult && Course?.CourseId == courseId)
        {
            Course = CopyCourse(
                Course,
                Course.CourseId,
                newStatus);
        }

        return Task.FromResult(StatusUpdateResult);
    }

    public Task<bool> UpdateStatusAsync(
        long courseId,
        string expectedStatus,
        string newStatus,
        IDbSession session,
        CancellationToken cancellationToken)
        => UpdateStatusAsync(courseId, expectedStatus, newStatus, cancellationToken);

    private static TrainingCourse CopyCourse(
        TrainingCourse source,
        long courseId,
        string courseStatus)
    {
        return new TrainingCourse
        {
            CourseId = courseId,
            CourseName = source.CourseName,
            CourseType = source.CourseType,
            DurationHours = source.DurationHours,
            TrainerId = source.TrainerId,
            TrainerName = source.TrainerName,
            MaxStudents = source.MaxStudents,
            StartAt = source.StartAt,
            EndAt = source.EndAt,
            Location = source.Location,
            CourseStatus = courseStatus,
            BudgetAmount = source.BudgetAmount,
            DeptId = source.DeptId,
            DeptName = source.DeptName,
            PreTestUrl = source.PreTestUrl,
            PostTestUrl = source.PostTestUrl,
            MaterialUrl = source.MaterialUrl,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }
}

/// <summary>课程发布测试用：模拟开启事务会话，并记录提交/回滚。</summary>
internal sealed class FakeDbConnectionFactory : IDbConnectionFactory
{
    public bool IsConfigured => true;

    public int BeginCallCount { get; private set; }

    public FakeDbSession? LastSession { get; private set; }

    public Task<System.Data.Common.DbConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken)
        => throw new NotSupportedException("课程发布必须通过 BeginSessionAsync 开启事务。");

    public Task<IDbSession> BeginSessionAsync(CancellationToken cancellationToken)
    {
        BeginCallCount++;
        LastSession = new FakeDbSession();
        return Task.FromResult<IDbSession>(LastSession);
    }
}

internal sealed class FakeDbSession : IDbSession
{
    public bool Committed { get; private set; }

    public bool RolledBack { get; private set; }

    public bool Disposed { get; private set; }

    public System.Data.Common.DbConnection Connection
        => throw new NotSupportedException("测试替身不提供真实连接。");

    public System.Data.Common.DbTransaction Transaction
        => throw new NotSupportedException("测试替身不提供真实事务。");

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Committed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        RolledBack = true;
        return Task.CompletedTask;
    }

    /// <summary>与 OracleDbSession 一致：未提交的会话在释放时回滚。</summary>
    public ValueTask DisposeAsync()
    {
        Disposed = true;

        if (!Committed)
        {
            RolledBack = true;
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>课程发布测试用的部门预算占用替身。</summary>
internal sealed class FakeDepartmentTrainingService : IDepartmentTrainingService
{
    public bool OccupyResult { get; set; } = true;

    public int OccupyCallCount { get; private set; }

    public long? LastDeptId { get; private set; }

    public decimal? LastAmount { get; private set; }

    public Task<bool> TryOccupyBudgetAsync(
        long deptId,
        decimal amount,
        IDbSession session,
        CancellationToken cancellationToken = default)
    {
        OccupyCallCount++;
        LastDeptId = deptId;
        LastAmount = amount;
        return Task.FromResult(OccupyResult);
    }

    public Task<PagedResult<DepartmentTrainingResponse>> GetPagedAsync(
        DepartmentTrainingQuery query,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<DepartmentTrainingResponse?> GetByIdAsync(
        long deptId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<DepartmentTrainingResponse> CreateAsync(
        CreateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<DepartmentTrainingResponse> UpdateAsync(
        long deptId,
        UpdateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<bool> DeleteAsync(
        long deptId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

internal sealed class FakeTrainerRepository : ITrainerRepository
{
    public Trainer? Trainer { get; set; }

    public IReadOnlyList<Trainer>? Trainers { get; set; }

    public bool ExistsResult { get; set; } = true;

    public bool UpdateResult { get; set; } = true;

    public long CreatedId { get; set; } = 200;

    public Trainer? LastUpdatedTrainer { get; private set; }

    public TrainerQuery? LastQuery { get; private set; }

    public Task<(IReadOnlyList<Trainer> Items, long Total)> GetAllAsync(
        TrainerQuery query,
        CancellationToken cancellationToken)
    {
        LastQuery = query;

        var filtered = (Trainers ?? (Trainer is null ? Array.Empty<Trainer>() : new[] { Trainer }))
            .Where(t => query.TrainerName is null || t.TrainerName.Contains(query.TrainerName))
            .Where(t => query.Company is null || (t.Company?.Contains(query.Company) ?? false))
            .Where(t => query.IsInternal is null || t.IsInternal == query.IsInternal)
            .OrderByDescending(t => t.TrainerId).ToArray();
        var offset = ((long)query.Page - 1) * query.PageSize;
        IReadOnlyList<Trainer> items = offset >= filtered.Length
            ? Array.Empty<Trainer>()
            : filtered.Skip((int)offset).Take(query.PageSize).ToArray();
        return Task.FromResult((items, (long)filtered.Length));
    }

    public Task<Trainer?> GetByIdAsync(
        long trainerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Trainer?.TrainerId == trainerId
                ? Trainer
                : null);
    }

    public Task<bool> ExistsAsync(
        long trainerId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ExistsResult);
    }

    public Task<long> CreateAsync(
        Trainer trainer,
        CancellationToken cancellationToken)
    {
        Trainer = CopyTrainer(
            trainer,
            CreatedId);

        return Task.FromResult(CreatedId);
    }

    public Task<bool> UpdateAsync(
        Trainer trainer,
        CancellationToken cancellationToken)
    {
        LastUpdatedTrainer = trainer;

        if (UpdateResult)
        {
            Trainer = CopyTrainer(
                trainer,
                trainer.TrainerId);
        }

        return Task.FromResult(UpdateResult);
    }

    private static Trainer CopyTrainer(
        Trainer source,
        long trainerId)
    {
        return new Trainer
        {
            TrainerId = trainerId,
            TrainerName = source.TrainerName,
            Title = source.Title,
            Company = source.Company,
            Phone = source.Phone,
            Email = source.Email,
            StarLevel = source.StarLevel,
            IsInternal = source.IsInternal,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }
}

internal sealed class FakeRegistrationRepository : IRegistrationRepository
{
    public RegistrationRecord? Registration { get; set; }

    public RegistrationContextRecord? Context { get; set; }

    public long? FiledRequestId { get; set; } = 5;

    public EmployeeEligibilityRecord? Employee { get; set; }

    public CourseEligibilityRecord? Course { get; set; }

    public RegistrationCreateResult CreateResult { get; set; } =
        new(RegistrationCreateOutcome.Created, 100);

    public bool CancelResult { get; set; } = true;

    public bool AbsentResult { get; set; } = true;

    public bool CompleteResult { get; set; } = true;

    public RegistrationSummaryRecord Summary { get; set; } = new();

    public RegistrationQuery? LastQuery { get; private set; }

    public long? LastScopeEmpId { get; private set; }

    public RegistrationRecord? LastCreated { get; private set; }

    public long? LastCancelRegId { get; private set; }

    public string? LastCancelReason { get; private set; }

    public long? LastAbsentRegId { get; private set; }

    public long? LastCompleteRegId { get; private set; }

    public Task<(IReadOnlyList<RegistrationRecord> Items, long Total)> GetAllAsync(
        RegistrationQuery query,
        long? empId,
        CancellationToken cancellationToken)
    {
        LastQuery = query;
        LastScopeEmpId = empId;

        IReadOnlyList<RegistrationRecord> items = Registration is null
            ? Array.Empty<RegistrationRecord>()
            : new[] { Registration };

        return Task.FromResult((items, (long)items.Count));
    }

    public Task<RegistrationRecord?> GetByIdAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Registration?.RegId == regId
                ? Registration
                : null);
    }

    public Task<RegistrationContextRecord?> GetContextAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Context?.RegId == regId
                ? Context
                : null);
    }

    public Task<long?> FindFiledRequestIdAsync(
        long empId,
        long courseId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(FiledRequestId);
    }

    public Task<EmployeeEligibilityRecord?> GetEmployeeEligibilityAsync(
        long empId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Employee);
    }

    public Task<CourseEligibilityRecord?> GetCourseEligibilityAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Course?.CourseId == courseId
                ? Course
                : null);
    }

    public Task<RegistrationCreateResult> CreateAsync(
        RegistrationRecord registration,
        CancellationToken cancellationToken)
    {
        LastCreated = registration;

        if (CreateResult.Outcome == RegistrationCreateOutcome.Created)
        {
            Registration = CopyRegistration(
                registration,
                CreateResult.RegId);
        }

        return Task.FromResult(CreateResult);
    }

    public Task<bool> CancelAsync(
        long regId,
        string? cancelReason,
        CancellationToken cancellationToken)
    {
        LastCancelRegId = regId;
        LastCancelReason = cancelReason;

        if (CancelResult
            && Registration?.RegId == regId)
        {
            Registration = CopyRegistration(
                Registration,
                Registration.RegId,
                status: "CANCELED",
                canceledAt: DateTime.Now,
                cancelReason: cancelReason);
        }

        return Task.FromResult(CancelResult);
    }

    public Task<bool> MarkAbsentAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        LastAbsentRegId = regId;

        if (AbsentResult
            && Registration?.RegId == regId)
        {
            Registration = CopyRegistration(
                Registration,
                Registration.RegId,
                status: "ABSENT",
                actualHours: 0m);
        }

        return Task.FromResult(AbsentResult);
    }

    public Task<bool> CompleteAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        LastCompleteRegId = regId;

        if (CompleteResult
            && Registration?.RegId == regId)
        {
            Registration = CopyRegistration(
                Registration,
                Registration.RegId,
                status: "COMPLETED",
                completedAt: DateTime.Now);
        }

        return Task.FromResult(CompleteResult);
    }

    public Task<RegistrationSummaryRecord> GetSummaryAsync(
        long? courseId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Summary);
    }

    private static RegistrationRecord CopyRegistration(
        RegistrationRecord source,
        long regId,
        string? status = null,
        decimal? actualHours = null,
        DateTime? completedAt = null,
        DateTime? canceledAt = null,
        string? cancelReason = null)
    {
        return new RegistrationRecord
        {
            RegId = regId,
            RequestId = source.RequestId,
            EmpId = source.EmpId,
            EmpName = source.EmpName,
            DeptId = source.DeptId,
            DeptName = source.DeptName,
            CourseId = source.CourseId,
            CourseName = source.CourseName,
            CourseType = source.CourseType,
            DurationHours = source.DurationHours,
            TrainerName = source.TrainerName,
            StartAt = source.StartAt,
            EndAt = source.EndAt,
            Location = source.Location,
            CourseStatus = source.CourseStatus,
            MaxStudents = source.MaxStudents,
            Status = status ?? source.Status,
            RegisteredAt = source.RegisteredAt,
            CompletedAt = completedAt ?? source.CompletedAt,
            ActualHours = actualHours ?? source.ActualHours,
            CanceledAt = canceledAt ?? source.CanceledAt,
            CancelReason = cancelReason ?? source.CancelReason,
            UpdatedAt = source.UpdatedAt,
            AttendId = source.AttendId,
            SigninType = source.SigninType,
            SignedInAt = source.SignedInAt,
            LatenessMinutes = source.LatenessMinutes,
            DeductHours = source.DeductHours,
            AttendanceRemark = source.AttendanceRemark
        };
    }
}

internal sealed class FakeAttendanceRepository : IAttendanceRepository
{
    public SignInResult Result { get; set; } =
        new(SignInOutcome.Success, 7);

    public AttendanceRecord? LastAttendance { get; private set; }

    public decimal LastActualHours { get; private set; }

    public Task<SignInResult> SignInAsync(
        AttendanceRecord attendance,
        decimal actualHours,
        CancellationToken cancellationToken)
    {
        LastAttendance = attendance;
        LastActualHours = actualHours;

        return Task.FromResult(Result);
    }
}

internal static class TestPrincipals
{
    public static ClaimsPrincipal Employee(long empId)
    {
        return Principal(
            empId,
            TrainingManagement.Api.Common.Security.RoleCodes.Employee);
    }

    public static ClaimsPrincipal Hr(long empId = 99)
    {
        return Principal(
            empId,
            TrainingManagement.Api.Common.Security.RoleCodes.Hr);
    }

    public static ClaimsPrincipal Admin(long empId = 100)
    {
        return Principal(
            empId,
            TrainingManagement.Api.Common.Security.RoleCodes.Admin);
    }

    private static ClaimsPrincipal Principal(
        long empId,
        string role)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    empId.ToString()),
                new Claim(
                    ClaimTypes.Role,
                    role)
            },
            "test");

        return new ClaimsPrincipal(identity);
    }
}
