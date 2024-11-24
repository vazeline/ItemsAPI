using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Utility
{
    public static class EventRateUtility
    {
        public static Action Debounce(Action action, int delayMs)
        {
            CancellationTokenSource lastCancellationToken = null;

            return () =>
            {
                // cancel/dispose previous
                lastCancellationToken?.Cancel();

                try
                {
                    lastCancellationToken?.Dispose();
                }

                // might happen due to thread issues, or application shutdown - do nothing
                catch
                {
                }

                var tokenSource = lastCancellationToken = new CancellationTokenSource();

                _ = Task
                    .Delay(delayMs)
                    .ContinueWith(
                        continuationAction: _ =>
                        {
                            action();
                        },
                        cancellationToken: tokenSource.Token,
                        continuationOptions: TaskContinuationOptions.DenyChildAttach,
                        scheduler: TaskScheduler.Default);
            };
        }
    }
}
