using System;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace EditorConfig
{
    internal static class JoinableTaskFactoryExtensions
    {
        /// <summary>
        /// Schedules a delegate for background execution on the UI thread without inheriting any claim to the UI thread from its caller.
        /// </summary>
        /// <param name="joinableTaskFactory">The factory to use for creating the task.</param>
        /// <param name="asyncMethod">The async delegate to invoke on the UI thread sometime in the future.</param>
        /// <param name="priority">The priority to use when switching to the UI thread or resuming after a yielding await.</param>
        /// <returns>The <see cref="JoinableTask"/> that represents the on-idle operation.</returns>
        public static JoinableTask StartOnIdle(this JoinableTaskFactory joinableTaskFactory, Func<Task> asyncMethod, VsTaskRunContext priority = VsTaskRunContext.UIThreadBackgroundPriority)
        {
            Requires.NotNull(joinableTaskFactory, nameof(joinableTaskFactory));
            Requires.NotNull(asyncMethod, nameof(asyncMethod));

            // Avoid inheriting any context from any ambient JoinableTask that is scheduling this work.
            using (joinableTaskFactory.Context.SuppressRelevance())
            {
                return joinableTaskFactory.RunAsync(
                    priority,
                    async () =>
                    {
                        // We always yield, so as to not inline execution of the delegate if the caller is already on the UI thread.
                        await Task.Yield();

                        // In case the caller wasn't on the UI thread, switch to it. It no-ops if we're already there.
                        await joinableTaskFactory.SwitchToMainThreadAsync();

                        await asyncMethod();
                    });
            }
        }
    }
}
