using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Narvi.Notify
{
    public class NotifyRegistrar
    {
        private Action _unregister = null;

        internal NotifyRegistrar(Action r)
        {
            _unregister = r;
        }

        public void Unregister() => _unregister?.Invoke();
    }
}
