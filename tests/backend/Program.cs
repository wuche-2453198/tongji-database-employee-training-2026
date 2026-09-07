using TrainingManagement.Api.ModuleTests;

var tests = CourseServiceTests
    .GetTests()
    .Concat(TrainerServiceTests.GetTests())
    .Concat(RequestValidationTests.GetTests())
    .Concat(RepositoryContractTests.GetTests())
    .ToArray();

if (args.Contains("--auth-http"))
{
    tests = tests.Append(("Course/trainer HTTP login and authorization matrix", (Func<Task>)HttpAuthorizationTests.RunAsync)).ToArray();
}

var failed = 0;

foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}");
        Console.WriteLine($"     {exception.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Total: {tests.Length}; passed: {tests.Length - failed}; failed: {failed}.");

return failed == 0
    ? 0
    : 1;
