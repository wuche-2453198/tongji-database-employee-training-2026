using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

/// <summary>
/// 成果评估模块规格场景:
/// 未完成发证、缺少必需 POST 成绩、重复证书、越权查询,以及 PRE/POST 录入时点(D-012)。
/// </summary>
internal static class ResultModuleTests
{
    private const long CompletedCourseId = 27;
    private const long PostTestCourseId = 101;
    private const long StartedCourseId = 102;
    private const long UnfinishedCourseId = 103;

    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Certificate rejects registration that is not COMPLETED", CertificateRejectsUncompletedAsync);
        yield return ("Certificate requires POST score when course configures post test", CertificateRequiresPostScoreAsync);
        yield return ("Certificate requires POST score at least 60", CertificateRequiresPostScore60Async);
        yield return ("Certificate rejects duplicates", CertificateRejectsDuplicateAsync);
        yield return ("Certificate detail blocks other employees", CertificateBlocksOtherEmployeeAsync);
        yield return ("Certificate detail allows HR and owner", CertificateAllowsHrAndOwnerAsync);
        yield return ("PRE test requires HR-filed request", PreTestRequiresHrFiledAsync);
        yield return ("PRE test rejects after course start", PreTestRejectsAfterStartAsync);
        yield return ("POST test rejects before course end", PostTestRejectsBeforeEndAsync);
        yield return ("Rating requires completed training", RatingRequiresCompletedAsync);
        yield return ("Rating rejects duplicate by employee and course", RatingRejectsDuplicateAsync);
        yield return ("Improvement reports null until both scores exist", ImprovementNullAwareAsync);
    }

    // ---------- 证书 ----------

    private static async Task CertificateRejectsUncompletedAsync()
    {
        var repository = new FakeCertificateRepository();
        repository.Registrations[5] = new Registration { RegId = 5, EmpId = 55, CourseId = (int)UnfinishedCourseId, Status = "REGISTERED" };
        var service = new CertificateService(repository);

        var exception = await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 5 }, IssuedBy),
            "未完成培训不应发证");
        TestAssert.True(exception.Message.Contains("尚未完成培训"), "应提示尚未完成培训");
    }

    private static async Task CertificateRequiresPostScoreAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.CourseGate = new ResultCourseGate { Exists = true, PostTestUrl = "https://test.example/post" };
        repository.PostScore = null;
        var service = new CertificateService(repository);

        var exception = await TestAssert.ThrowsAsync<BusinessException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy),
            "缺 POST 成绩不应发证");
        TestAssert.True(exception.Message.Contains("POST"), "应提示缺 POST 成绩");
    }

    private static async Task CertificateRequiresPostScore60Async()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.CourseGate = new ResultCourseGate { Exists = true, PostTestUrl = "https://test.example/post" };
        repository.PostScore = 59m;
        var service = new CertificateService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy),
            "POST 不足 60 分不应发证");

        repository.PostScore = 60m;
        var result = await service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy);
        TestAssert.True(result.CertificateId > 0, "POST 恰好 60 分应发证");
    }

    private static async Task CertificateRejectsDuplicateAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.Certificates.Add(new TrainingCertificate { CertId = 9, EmpId = 55, CourseId = (int)CompletedCourseId });
        var service = new CertificateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.GenerateCertificateAsync(new GenerateCertificateRequest { RegistrationId = 1 }, IssuedBy),
            "重复发证应拒绝");
    }

    private static async Task CertificateBlocksOtherEmployeeAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.Certificates.Add(new TrainingCertificate { CertId = 9, EmpId = 58, CourseId = (int)CompletedCourseId });
        var service = new CertificateService(repository);

        var actor = new ActorContext(55, new[] { "EMPLOYEE" });
        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.GetCertificateByIdAsync(actor, 9),
            "员工查看他人证书应 403");
    }

    private static async Task CertificateAllowsHrAndOwnerAsync()
    {
        var repository = CreateRepoWithCompletedRegistration();
        repository.Certificates.Add(new TrainingCertificate { CertId = 9, EmpId = 55, CourseId = (int)CompletedCourseId });
        var service = new CertificateService(repository);

        var owner = await service.GetCertificateByIdAsync(new ActorContext(55, new[] { "EMPLOYEE" }), 9);
        TestAssert.Equal(9, owner.CertId, "本人可查");

        var hr = await service.GetCertificateByIdAsync(new ActorContext(57, new[] { "HR" }), 9);
        TestAssert.Equal(9, hr.CertId, "HR 可查");

        var admin = await service.GetCertificateByIdAsync(new ActorContext(54, new[] { "ADMIN" }), 9);
        TestAssert.Equal(9, admin.CertId, "管理员可查");
    }

    // ---------- 成绩 ----------

    private static async Task PreTestRequiresHrFiledAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddDays(7), EndAt = DateTime.Now.AddDays(8) },
            HasHrFiled = false,
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)CompletedCourseId, TestType = "PRE", Score = 70 }, 57),
            "未备案不能录 PRE");
    }

    private static async Task PreTestRejectsAfterStartAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddHours(-1), EndAt = DateTime.Now.AddDays(1) },
            HasHrFiled = true,
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)StartedCourseId, TestType = "PRE", Score = 70 }, 57),
            "开课后不能录 PRE");
    }

    private static async Task PostTestRejectsBeforeEndAsync()
    {
        var repository = new FakeTestRepository
        {
            CourseGate = new ResultCourseGate { Exists = true, StartAt = DateTime.Now.AddDays(-1), EndAt = DateTime.Now.AddDays(1) },
            HasHrFiled = true,
        };
        var service = new TestService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateTestAsync(new CreateTestRequest { EmpId = 55, CourseId = (int)StartedCourseId, TestType = "POST", Score = 90 }, 57),
            "课程未结束不能录 POST");
    }

    private static async Task ImprovementNullAwareAsync()
    {
        var repository = new FakeTestRepository();
        repository.Scores[(55, 30)] = (65m, null);
        var service = new TestService(repository);

        var partial = await service.GetImprovementAsync(55, 30);
        TestAssert.True(partial.Improvement is null, "缺 POST 时提升值应为 null");

        repository.Scores[(55, 30)] = (65m, 95m);
        var full = await service.GetImprovementAsync(55, 30);
        TestAssert.Equal(30m, full.Improvement, "提升值应为 POST - PRE");
    }

    // ---------- 评分 ----------

    private static async Task RatingRequiresCompletedAsync()
    {
        var repository = new FakeRatingRepository { HasCompleted = false };
        var service = new RatingService(repository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateRatingAsync(new CreateRatingRequest { CourseId = (int)UnfinishedCourseId, TrainerId = 1, Score = 5 }, 55),
            "未完成培训不能评分");
    }

    private static async Task RatingRejectsDuplicateAsync()
    {
        var repository = new FakeRatingRepository { HasCompleted = true, ExistsRating = true };
        var service = new RatingService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateRatingAsync(new CreateRatingRequest { CourseId = (int)CompletedCourseId, TrainerId = 1, Score = 5 }, 55),
            "重复评分应拒绝");
    }

    // ---------- 夹具 ----------

    private const int IssuedBy = 57;

    private static FakeCertificateRepository CreateRepoWithCompletedRegistration()
    {
        var repository = new FakeCertificateRepository();
        repository.Registrations[1] = new Registration { RegId = 1, EmpId = 55, CourseId = (int)CompletedCourseId, Status = "COMPLETED" };
        repository.CourseGate = new ResultCourseGate { Exists = true, PostTestUrl = "https://test.example/post" };
        repository.PostScore = 88m;
        return repository;
    }

    private sealed class FakeCertificateRepository : Repositories.Interfaces.ICertificateRepository
    {
        public Dictionary<int, Registration> Registrations { get; } = new();

        public List<TrainingCertificate> Certificates { get; } = new();

        public ResultCourseGate CourseGate { get; set; } = new() { Exists = true };

        public decimal? PostScore { get; set; }

        public int NextCertId { get; set; } = 100;

        public Task<Registration?> GetRegistrationByIdAsync(int registrationId)
        {
            Registrations.TryGetValue(registrationId, out var registration);
            return Task.FromResult(registration);
        }

        public Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            return Task.FromResult(Certificates.Any(c => c.EmpId == employeeId && c.CourseId == courseId));
        }

        public Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int issuedByEmpId)
        {
            var certId = ++NextCertId;
            Certificates.Add(new TrainingCertificate
            {
                CertId = certId,
                CertCode = certificateNo,
                EmpId = employeeId,
                CourseId = courseId,
                IssuedByEmpId = issuedByEmpId,
                Notified = "N",
            });
            return Task.FromResult(certId);
        }

        public Task<IEnumerable<TrainingCertificate>> GetByEmployeeIdAsync(int employeeId)
        {
            return Task.FromResult<IEnumerable<TrainingCertificate>>(
                Certificates.Where(c => c.EmpId == employeeId).ToArray());
        }

        public Task<TrainingCertificate?> GetByIdAsync(int id)
        {
            return Task.FromResult(Certificates.FirstOrDefault(c => c.CertId == id));
        }

        public Task<bool> UpdateNotifyFlagAsync(int id)
        {
            var certificate = Certificates.FirstOrDefault(c => c.CertId == id);
            if (certificate is null) return Task.FromResult(false);
            certificate.Notified = "Y";
            certificate.NotifiedAt = DateTime.Now;
            return Task.FromResult(true);
        }

        public Task<ResultCourseGate> GetCourseGateAsync(int courseId)
        {
            return Task.FromResult(CourseGate);
        }

        public Task<decimal?> GetPostTestScoreAsync(int employeeId, int courseId)
        {
            return Task.FromResult(PostScore);
        }
    }

    private sealed class FakeTestRepository : Repositories.Interfaces.ITestRepository
    {
        public ResultCourseGate CourseGate { get; set; } = new() { Exists = true };

        public bool HasHrFiled { get; set; }

        public Dictionary<(int EmpId, int CourseId), (decimal? Pre, decimal? Post)> Scores { get; } = new();

        public Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId)
        {
            Scores.TryGetValue((request.EmpId, request.CourseId), out var current);
            var score = request.TestType == "PRE" ? (request.Score, current.Post) : (current.Pre, (decimal?)request.Score);
            Scores[(request.EmpId, request.CourseId)] = score;
            return Task.FromResult(true);
        }

        public Task<bool> UpdateScoreAsync(CreateTestRequest request, int recordedByEmpId)
        {
            return CreateAsync(request, recordedByEmpId);
        }

        public Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType)
        {
            Scores.TryGetValue((employeeId, courseId), out var current);
            var exists = testType == "PRE" ? current.Pre is not null : current.Post is not null;
            return Task.FromResult(exists);
        }

        public Task<PagedResult<TrainingTest>> GetPagedListAsync(
            int? employeeId, int? courseId, string? testType, int page, int pageSize)
        {
            return Task.FromResult(new PagedResult<TrainingTest>(Array.Empty<TrainingTest>(), page, pageSize, 0));
        }

        public Task<ResultCourseGate> GetCourseGateAsync(int courseId)
        {
            return Task.FromResult(CourseGate);
        }

        public Task<bool> HasHrFiledRequestAsync(int employeeId, int courseId)
        {
            return Task.FromResult(HasHrFiled);
        }

        public Task<(decimal? PreScore, decimal? PostScore)> GetScoresAsync(int employeeId, int courseId)
        {
            Scores.TryGetValue((employeeId, courseId), out var current);
            return Task.FromResult(current);
        }
    }

    private sealed class FakeRatingRepository : Repositories.Interfaces.IRatingRepository
    {
        public bool HasCompleted { get; set; }

        public bool ExistsRating { get; set; }

        public List<TrainerRating> Ratings { get; } = new();

        public Task<bool> CreateAsync(Dtos.Ratings.CreateRatingRequest request, int employeeId)
        {
            Ratings.Add(new TrainerRating { RatingId = Ratings.Count + 1, EmpId = employeeId, CourseId = request.CourseId });
            return Task.FromResult(true);
        }

        public Task<(IReadOnlyList<TrainerRating> Items, int Total)> GetListAsync(
            int? courseId, int? trainerId, int page, int pageSize)
        {
            IReadOnlyList<TrainerRating> items = Ratings;
            return Task.FromResult((items, Ratings.Count));
        }

        public Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            return Task.FromResult(ExistsRating);
        }

        public Task<TrainerRating?> GetByIdAsync(int ratingId)
        {
            return Task.FromResult(Ratings.FirstOrDefault(r => r.RatingId == ratingId));
        }

        public Task<bool> VerifyAsync(int ratingId, string verifyComment, int hrVerifierEmpId)
        {
            var rating = Ratings.FirstOrDefault(r => r.RatingId == ratingId);
            if (rating is null) return Task.FromResult(false);
            rating.HrVerified = "Y";
            return Task.FromResult(true);
        }

        public Task<bool> HasCompletedRegistrationAsync(int employeeId, int courseId)
        {
            return Task.FromResult(HasCompleted);
        }

        public Task<(decimal? AverageScore, int RatingCount)> GetAverageAsync(int courseId, int? trainerId)
        {
            var scores = Ratings.Where(r => r.CourseId == courseId).Select(r => (decimal)r.Score).ToArray();
            if (scores.Length == 0) return Task.FromResult(((decimal?)null, 0));
            return Task.FromResult(((decimal?)scores.Average(), scores.Length));
        }
    }
}
