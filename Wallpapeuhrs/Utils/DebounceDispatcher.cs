using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Wallpapeuhrs.Utils
{
    public class DebounceDispatcher
    {
        private Timer timer;

        public void Debounce(int interval, Action action)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            timer = new Timer(interval);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                action();
            };
            timer.AutoReset = false;
            timer.Start();
        }
    }
}
