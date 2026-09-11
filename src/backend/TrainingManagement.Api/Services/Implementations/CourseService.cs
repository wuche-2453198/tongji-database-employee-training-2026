using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class CourseService : ICourseService
{
    private const string DraftStatus = TrainingManagement.Api.Common.Enums.CourseStatusText.Draft;
    private const string PublishedStatus = TrainingManagement.Api.Common.Enums.CourseStatusText.Published;
    private const string ClosedStatus = TrainingManagement.Api.Common.Enums.CourseStatusText.Closed;

    private static readonly string[] CourseTypes =
    [
        "技术培训",
        "管理培训",
        "产品培训",
        "营销培训"
    ];

    private readonly ICourseRepository _courseRepository;
    private readonly ITrainerRepository _trainerRepository;
    private readonly IDepartmentTrainingService _departmentTrainingService;
    private readonly IDbConnectionFactory _connectionFactory;

    public CourseService(
        ICourseRepository courseRepository,
        ITrainerRepository trainerRepository,
        IDepartmentTrainingService departmentTrainingService,
        IDbConnectionFactory connectionFactory)
    {
        _courseRepository = courseRepository;
        _trainerRepository = trainerRepository;
        _departmentTrainingService = departmentTrainingService;
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<CourseResponse>> GetAllAsync(
        CourseQuery query,
        CancellationToken cancellationToken)
    {
        if (query.StartAtFrom.HasValue
            && query.StartAtTo.HasValue
            && query.StartAtFrom > query.StartAtTo)
        {
            throw new BusinessException(
                "查询开始时间不能晚于查询结束时间。");
        }

        var normalizedQuery = new CourseQuery
        {
            CourseName = TrimToNull(query.CourseName),
            CourseType = TrimToNull(query.CourseType),
            CourseStatus = TrimToNull(query.CourseStatus)?.ToUpperInvariant(),
            StartAtFrom = query.StartAtFrom,
            StartAtTo = query.StartAtTo,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var result = await _courseRepository.GetAllAsync(
            normalizedQuery,
            cancellationToken);

        var items = result.Items
            .Select(ToResponse)
            .ToArray();

        return new PagedResult<CourseResponse>(
            items,
            normalizedQuery.Page,
            normalizedQuery.PageSize,
            result.Total);
    }

    public async Task<CourseResponse?> GetByIdAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (course is null)
        {
            return null;
        }

        return ToResponse(course);
    }

    public async Task<CourseResponse> CreateAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        await CheckCreateRequestAsync(
            request,
            cancellationToken);

        var course = new TrainingCourse
        {
            CourseName = request.CourseName.Trim(),
            CourseType = request.CourseType.Trim(),
            DurationHours = request.DurationHours,
            TrainerId = request.TrainerId,
            MaxStudents = request.MaxStudents,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Location = TrimToNull(request.Location),
            CourseStatus = DraftStatus,
            BudgetAmount = request.BudgetAmount,
            DeptId = request.DeptId,
            PreTestUrl = TrimToNull(request.PreTestUrl),
            PostTestUrl = TrimToNull(request.PostTestUrl),
            MaterialUrl = TrimToNull(request.MaterialUrl)
        };

        var newId = await _courseRepository.CreateAsync(
            course,
            cancellationToken);

        var created = await _courseRepository.GetByIdAsync(
            newId,
            cancellationToken);

        if (created is null)
        {
            throw new BusinessException(
                "课程创建失败。");
        }

        return ToResponse(created);
    }

    public async Task<CourseResponse> UpdateAsync(
        long courseId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var oldCourse = await _courseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (oldCourse is null)
        {
            throw new NotFoundApiException(
                "课程不存在。");
        }

        CheckCourseCanBeUpdated(
            oldCourse,
            request);

        await CheckUpdateRequestAsync(
            request,
            cancellationToken);

        var newCourse = new TrainingCourse
        {
            CourseId = courseId,
            CourseName = request.CourseName.Trim(),
            CourseType = request.CourseType.Trim(),
            DurationHours = request.DurationHours,
            TrainerId = request.TrainerId,
            MaxStudents = request.MaxStudents,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Location = TrimToNull(request.Location),
            BudgetAmount = request.BudgetAmount,
            DeptId = request.DeptId,
            PreTestUrl = TrimToNull(request.PreTestUrl),
            PostTestUrl = TrimToNull(request.PostTestUrl),
            MaterialUrl = TrimToNull(request.MaterialUrl)
        };

        var success = await _courseRepository.UpdateAsync(
            newCourse,
            oldCourse.CourseStatus,
            cancellationToken);

        if (!success)
        {
            throw new ConflictApiException(
                "课程状态已发生变化，请刷新后重试。");
        }

        var updated = await _courseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (updated is null)
        {
            throw new NotFoundApiException(
                "课程不存在。");
        }

        return ToResponse(updated);
    }

    public async Task<(int MaxStudents, int ValidRegistrationCount, int RemainingSeats)?> GetCapacityAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var capacity = await _courseRepository.GetCapacityAsync(
            courseId,
            cancellationToken);

        if (capacity is null)
        {
            return null;
        }

        return (
            capacity.Value.MaxStudents,
            capacity.Value.ValidRegistrationCount,
            Math.Max(
                0,
                capacity.Value.MaxStudents - capacity.Value.ValidRegistrationCount));
    }

    public async Task<PublishCourseResponse> PublishAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (course is null)
        {
            throw new NotFoundApiException(
                "课程不存在。");
        }

        if (!IsStatus(course, DraftStatus))
        {
            throw new BusinessException(
                "只有草稿课程可以发布。");
        }

        CheckPublishRequiredFields(course);

        var deptId = course.DeptId
            ?? throw new BusinessException("课程缺少主办部门，不能发布。");

        // D-011：课程发布与部门预算占用必须在同一事务内提交，任一失败整体回滚。
        await using var session = await _connectionFactory.BeginSessionAsync(
            cancellationToken);

        var published = await _courseRepository.UpdateStatusAsync(
            courseId,
            DraftStatus,
            PublishedStatus,
            session,
            cancellationToken);

        if (!published)
        {
            throw new ConflictApiException(
                "课程状态已发生变化，请刷新后重试。");
        }

        var occupied = await _departmentTrainingService.TryOccupyBudgetAsync(
            deptId,
            course.BudgetAmount,
            session,
            cancellationToken);

        if (!occupied)
        {
            throw new ConflictApiException(
                "主办部门不存在或年度预算不足，无法发布课程。");
        }

        await session.CommitAsync(cancellationToken);

        var publishedCourse = await _courseRepository.GetByIdAsync(
            courseId,
            cancellationToken)
            ?? throw new BusinessException("课程发布后读取失败。");

        return ToPublishResponse(publishedCourse);
    }

    public async Task CloseAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        var course = await _courseRepository.GetByIdAsync(
            courseId,
            cancellationToken);

        if (course is null)
        {
            throw new NotFoundApiException(
                "课程不存在。");
        }

        if (!IsStatus(course, PublishedStatus))
        {
            throw new BusinessException(
                "只有已发布课程可以关闭。");
        }

        var success = await _courseRepository.UpdateStatusAsync(
            courseId,
            PublishedStatus,
            ClosedStatus,
            cancellationToken);

        if (!success)
        {
            throw new ConflictApiException(
                "课程状态已发生变化，请刷新后重试。");
        }
    }

    private async Task CheckCreateRequestAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        CheckBasicFields(
            request.CourseName,
            request.CourseType,
            request.DurationHours,
            request.MaxStudents,
            request.StartAt,
            request.EndAt,
            request.BudgetAmount,
            request.TrainerId,
            request.DeptId);

        await CheckTrainerExistsAsync(
            request.TrainerId,
            cancellationToken);

        await CheckDepartmentExistsAsync(request.DeptId, cancellationToken);
    }

    private async Task CheckUpdateRequestAsync(
        UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        CheckBasicFields(
            request.CourseName,
            request.CourseType,
            request.DurationHours,
            request.MaxStudents,
            request.StartAt,
            request.EndAt,
            request.BudgetAmount,
            request.TrainerId,
            request.DeptId);

        await CheckTrainerExistsAsync(
            request.TrainerId,
            cancellationToken);

        await CheckDepartmentExistsAsync(request.DeptId, cancellationToken);
    }

    private static void CheckBasicFields(
        string courseName,
        string courseType,
        decimal durationHours,
        int maxStudents,
        DateTime startAt,
        DateTime endAt,
        decimal budgetAmount,
        long trainerId,
        long deptId)
    {
        if (string.IsNullOrWhiteSpace(courseName))
        {
            throw new BusinessException(
                "课程名称不能为空。");
        }

        if (!CourseTypes.Contains(courseType.Trim()))
        {
            throw new BusinessException(
                "课程类型只能是技术培训、管理培训、产品培训、营销培训。");
        }

        if (durationHours < 0.1m || durationHours > 999.9m)
        {
            throw new BusinessException(
                "学时必须在0.1到999.9之间。");
        }

        if (maxStudents < 1 || maxStudents > 999999)
        {
            throw new BusinessException(
                "最大人数必须在1到999999之间。");
        }

        if (startAt >= endAt)
        {
            throw new BusinessException(
                "课程开始时间必须早于结束时间。");
        }

        if (budgetAmount < 0)
        {
            throw new BusinessException(
                "预算金额不能为负数。");
        }

        if (trainerId <= 0)
        {
            throw new BusinessException(
                "必须绑定讲师。");
        }

        if (deptId <= 0)
        {
            throw new BusinessException(
                "必须绑定主办部门。");
        }
    }

    private async Task CheckTrainerExistsAsync(
        long trainerId,
        CancellationToken cancellationToken)
    {
        var exists = await _trainerRepository.ExistsAsync(
            trainerId,
            cancellationToken);

        if (!exists)
        {
            throw new BusinessException(
                "绑定的讲师不存在。");
        }
    }

    private async Task CheckDepartmentExistsAsync(long deptId, CancellationToken cancellationToken)
    {
        if (!await _courseRepository.DepartmentExistsAsync(deptId, cancellationToken))
        {
            throw new BusinessException("绑定的主办部门不存在。");
        }
    }

    private static void CheckCourseCanBeUpdated(
        TrainingCourse oldCourse,
        UpdateCourseRequest request)
    {
        if (IsStatus(oldCourse, ClosedStatus))
        {
            throw new BusinessException(
                "课程已关闭，不能再修改。");
        }

        if (IsStatus(oldCourse, DraftStatus))
        {
            return;
        }

        if (IsStatus(oldCourse, PublishedStatus))
        {
            CheckPublishedCourseUpdate(
                oldCourse,
                request);

            return;
        }

        throw new BusinessException(
            "课程状态不合法，不能修改。");
    }

    private static void CheckPublishedCourseUpdate(
        TrainingCourse oldCourse,
        UpdateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Location))
        {
            throw new BusinessException("已发布课程的地点不能为空。");
        }

        if (!EqualsIgnoreTrim(
                oldCourse.CourseName,
                request.CourseName)
            || !EqualsIgnoreTrim(
                oldCourse.CourseType,
                request.CourseType)
            || oldCourse.TrainerId != request.TrainerId
            || oldCourse.DeptId != request.DeptId
            || oldCourse.StartAt != request.StartAt
            || oldCourse.EndAt != request.EndAt
            || oldCourse.MaxStudents != request.MaxStudents
            || oldCourse.DurationHours != request.DurationHours
            || oldCourse.BudgetAmount != request.BudgetAmount)
        {
            throw new BusinessException(
                "课程发布后不能修改名称、类型、讲师、部门、时间、容量、学时和预算。");
        }

        if (oldCourse.StartAt <= DateTime.Now
            && !EqualsIgnoreTrim(
                oldCourse.Location,
                request.Location))
        {
            throw new BusinessException(
                "课程开始后不能修改地点。");
        }
    }

    private static void CheckPublishRequiredFields(
        TrainingCourse course)
    {
        if (string.IsNullOrWhiteSpace(course.CourseName)
            || string.IsNullOrWhiteSpace(course.CourseType)
            || course.DurationHours <= 0
            || course.MaxStudents <= 0
            || course.StartAt is null
            || course.EndAt is null
            || course.StartAt >= course.EndAt
            || string.IsNullOrWhiteSpace(course.Location)
            || course.TrainerId is null or <= 0
            || course.DeptId is null or <= 0
            || course.BudgetAmount < 0)
        {
            throw new BusinessException(
                "课程信息不完整，不能发布。");
        }

        if (!CourseTypes.Contains(
                course.CourseType.Trim()))
        {
            throw new BusinessException(
                "课程类型不合法，不能发布。");
        }
    }

    private static CourseResponse ToResponse(
        TrainingCourse course)
    {
        return new CourseResponse
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            CourseType = course.CourseType,
            DurationHours = course.DurationHours,
            TrainerId = course.TrainerId,
            TrainerName = course.TrainerName,
            MaxStudents = course.MaxStudents,
            RegisteredCount = course.RegisteredCount,
            RemainingSeats = course.RegisteredCount.HasValue
                ? Math.Max(0, course.MaxStudents - course.RegisteredCount.Value)
                : null,
            StartAt = course.StartAt,
            EndAt = course.EndAt,
            Location = course.Location,
            CourseStatus = course.CourseStatus,
            BudgetAmount = course.BudgetAmount,
            DeptId = course.DeptId,
            DeptName = course.DeptName,
            PreTestUrl = course.PreTestUrl,
            PostTestUrl = course.PostTestUrl,
            MaterialUrl = course.MaterialUrl,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }

    private static PublishCourseResponse ToPublishResponse(
        TrainingCourse course)
    {
        var response = ToResponse(course);

        return new PublishCourseResponse
        {
            Published = true,
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            CourseStatus = course.CourseStatus,
            MaxStudents = course.MaxStudents,
            RegisteredCount = course.RegisteredCount ?? 0,
            RemainingSeats = response.RemainingSeats ?? 0,
            PublishTime = course.UpdatedAt
        };
    }

    private static bool IsStatus(
        TrainingCourse course,
        string status)
    {
        return string.Equals(
            course.CourseStatus,
            status,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool EqualsIgnoreTrim(
        string? left,
        string? right)
    {
        return string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.Ordinal);
    }

    private static string? TrimToNull(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
