using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// PureCrossPlatformLocaleWatcher.cs
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Threading;

namespace Tlumach.Sample.Avalonia
{

    internal sealed class LocaleWatcher : IDisposable
    {
        private readonly TimeSpan _pollInterval;
        private CancellationTokenSource? _cts;

        private string _lastCulture = CultureInfo.CurrentCulture.Name;
        private string _lastUICulture = CultureInfo.CurrentUICulture.Name;

        public event EventHandler? SystemLocaleChanged;

        public LocaleWatcher(TimeSpan? pollInterval = null)
        {
            _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        }

        public void Start()
        {
            if (_cts != null) return;

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task PollLoop(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(_pollInterval);

            while (!ct.IsCancellationRequested)
            {
                CheckAndRaise();

                try
                {
                    await timer.WaitForNextTickAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void CheckAndRaise()
        {
            var c = CultureInfo.CurrentCulture.Name;
            var ui = CultureInfo.CurrentUICulture.Name;

            if (!string.Equals(c, _lastCulture, StringComparison.Ordinal) || !string.Equals(ui, _lastUICulture, StringComparison.Ordinal))
            {
                _lastCulture = c;
                _lastUICulture = ui;

                Dispatcher.UIThread.Post(() => SystemLocaleChanged?.Invoke(this, EventArgs.Empty));
            }
        }
    }
}
