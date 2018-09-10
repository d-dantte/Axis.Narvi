using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Axis.Narvi.Core.Notify
{
    public interface INotifiable : INotifyPropertyChanged
    {
        void Notify([CallerMemberName] string propertyName = null);
        void Notify(Expression<Func<object>> exp);
    }
}
