using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

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
