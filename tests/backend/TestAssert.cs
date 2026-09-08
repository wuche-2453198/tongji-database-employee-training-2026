namespace TrainingManagement.Api.ModuleTests;

internal static class TestAssert
{
    public static void True(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(
        T expected,
        T actual,
        string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected: {expected}; actual: {actual}.");
        }
    }

    public static async Task<TException> ThrowsAsync<TException>(
        Func<Task> action,
        string message)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"{message} Expected {typeof(TException).Name}, but got {exception.GetType().Name}.",
                exception);
        }

        throw new InvalidOperationException(
            $"{message} Expected {typeof(TException).Name}, but no exception was thrown.");
    }
}
