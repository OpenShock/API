using Serilog;
using System.Runtime.CompilerServices;

namespace OpenShock.Common.Utils;

public static class OsTask
{
    public static Task Run(Func<Task?> function, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1)
    {
        var task = Task.Run(function);
        task.ContinueWith(t => ErrorHandleTask(file, member, line, t), TaskContinuationOptions.OnlyOnFaulted);
        return task;
    }

    private static void ErrorHandleTask(string file, string member, int line, Task t)
    {
        if (!t.IsFaulted) return;
        var index = file.LastIndexOf('\\');
        if (index == -1) index = file.LastIndexOf('/');
        Log.Error(t.Exception,
            "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
            file[(index + 1)..], member, line, t.Exception?.StackTrace);
    }

    public static Task Run(Task? function, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1)
    {
        var task = Task.Run(() => function);
        task.ContinueWith(
            t => ErrorHandleTask(file, member, line, t), TaskContinuationOptions.OnlyOnFaulted);
        return task;
    }
}