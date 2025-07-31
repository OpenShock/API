using Serilog;
using System.Runtime.CompilerServices;

namespace OpenShock.Common.Utils;

public static class OsTask
{
    public static Task Run(Func<Task> function, [CallerFilePath] string path = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1)
    {
        var task = Task.Run(function);
        task.ContinueWith(t => ErrorHandleTask(path, member, line, t), TaskContinuationOptions.OnlyOnFaulted);
        return task;
    }

    private static void ErrorHandleTask(string path, string member, int line, Task t)
    {
        var file = Path.GetFileName(path);
        Log.Error(t.Exception,
            "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
            file, member, line, t.Exception?.StackTrace);
    }

    public static Task Run(Task function, [CallerFilePath] string path = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1)
    {
        var task = Task.Run(() => function);
        task.ContinueWith(
            t => ErrorHandleTask(path, member, line, t), TaskContinuationOptions.OnlyOnFaulted);
        return task;
    }
}