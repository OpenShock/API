using Serilog;
using System.Runtime.CompilerServices;

namespace OpenShock.Common.Utils;

public static class LucTask
{
    public static Task Run(Func<Task?> function, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1) => Task.Run(function).ContinueWith(
        t =>
        {
            if (!t.IsFaulted) return;
            var index = file.LastIndexOf('\\');
            if (index == -1) index = file.LastIndexOf('/');
            Log.Error(t.Exception,
                "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
                file[(index + 1)..], member, line, t.Exception?.StackTrace);
            
        }, TaskContinuationOptions.OnlyOnFaulted);

    public static Task Run(Task? function, [CallerFilePath] string file = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = -1) => Task.Run(() => function).ContinueWith(
        t =>
        {
            if (!t.IsFaulted) return;
            var index = file.LastIndexOf('\\');
            if (index == -1) index = file.LastIndexOf('/');
            Log.Error(t.Exception,
                "Error during task execution. {File}::{Member}:{Line} - Stack: {Stack}",
                file[(index + 1)..], member, line, t.Exception?.StackTrace);
            
        }, TaskContinuationOptions.OnlyOnFaulted);
}