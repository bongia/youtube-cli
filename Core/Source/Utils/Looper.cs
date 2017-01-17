using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MasDev.YouTube
{
    /// <summary>
    ///  This class can be used to invoke recurrent scheduled events
    /// </summary>
    public class Looper
    {
        public event Action<TimeSpan> Loop;
        readonly int _millisecondsInterval;
        readonly Stopwatch _timer;
        bool _isEnabled;

        public Looper(int millisecondsInterval)
        {
            _millisecondsInterval = millisecondsInterval;
            _timer = new Stopwatch();
        }

        public void Stop()
        {
            lock (_timer)
            {
                _timer.Stop();
                _isEnabled = false;
            }
        }

        public void Start()
        {
            lock (_timer)
            {
                if (_isEnabled)
                    throw new NotSupportedException("Must stop first");
                _isEnabled = true;
                _timer.Restart();
                Task.Run(async () => await LoopAsync());
            }
        }

        private async Task LoopAsync()
        {
            while (_isEnabled)
            {
                await Task.Delay(_millisecondsInterval);
                Loop?.Invoke(_timer.Elapsed);
            }
        }
    }
}