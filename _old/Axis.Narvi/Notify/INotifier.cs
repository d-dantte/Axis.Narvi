using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Axis.Narvi.Notify
{
    public interface INotifier : INotifyPropertyChanged
    {
        void notify([CallerMemberName] string propertyName = null);
        void notify(Expression<Func<object>> exp);
    }
}
