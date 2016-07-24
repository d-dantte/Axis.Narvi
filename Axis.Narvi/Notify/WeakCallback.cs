using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Axis.Luna.Extensions.ExceptionExtensions;

namespace Axis.Narvi.Notify
{
    public sealed class WeakCallback<EventArg>
    {
        private WeakReference<object> _targetRef = null;
        private Action<object, object, object> _targetDelegate = null;

        private WeakReference _sourceRef = null;
        private Func<Delegate> _sourceDelegate = null;

        public WeakCallback(Delegate callback, object source, Func<Delegate> sourceDelegate)
        {
            ThrowNullArguments(() => callback, () => source, () => sourceDelegate);

            _sourceRef = new WeakReference(source);
            _sourceDelegate = sourceDelegate;
            _targetRef = new WeakReference<object>(callback.Target);
            var evtType = typeof(EventArg);

            // (t, s, a) => t.SomeMethod(s, a); or Something(s, a);
            var t = Expression.Parameter(typeof(object), "target");
            var s = Expression.Parameter(typeof(object), "source");
            var a = Expression.Parameter(evtType, "args");
            
            var callExp = callback.Method.IsStatic ?
                          Expression.Call(callback.Method, s, Expression.Convert(a, evtType)) :
                          Expression.Call(Expression.Convert(t, callback.Target.GetType()), callback.Method, s, Expression.Convert(a, evtType));

            var lambda = Expression.Lambda(callExp, t, s, a); //<- works irrespective of the method being static or instance

            _targetDelegate = (Action<object, object, object>)lambda.Compile();
        }

        public void Invoke(object source, EventArg args)
        {
            object t = null;
            if (!_targetRef.TryGetTarget(out t) && _sourceRef.IsAlive) _sourceDelegate. -= this.Invoke;
            else _targetDelegate.Invoke(t, source, args);
        }

        public static void Remove(Delegate sourceDelegate, Delegate target)
        {

        }
    }
}
