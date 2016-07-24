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
    public sealed class WeakCallback<Delegate>
    where Delegate: class
    {
        private WeakReference<object> _targetRef = null;
        private Action<object, object, object> _targetDelegate = null;

        private WeakReference _sourceRef = null;
        private Action<WeakCallback<Delegate>> _removeAction = null;

        public WeakCallback(System.Delegate callback, object source, Action<WeakCallback<Delegate>> removeDelegateAction)
        {
            ThrowNullArguments(() => callback, () => source, () => removeDelegateAction);

            _sourceRef = new WeakReference(source);
            _removeAction = removeDelegateAction;
            _targetRef = new WeakReference<object>(callback.Target);
            var eventArgParam = callback.Method.GetParameters().Last();

            // (t, s, a) => t.SomeMethod(s, a); or Something(s, a);
            var t = Expression.Parameter(typeof(object), "target");
            var s = Expression.Parameter(typeof(object), "source");
            var a = Expression.Parameter(eventArgParam.ParameterType, "args");
            
            var callExp = callback.Method.IsStatic ?
                          Expression.Call(callback.Method, s, Expression.Convert(a, eventArgParam.ParameterType)) :
                          Expression.Call(Expression.Convert(t, callback.Target.GetType()), callback.Method, s, Expression.Convert(a, eventArgParam.ParameterType));

            var lambda = Expression.Lambda(callExp, t, s, a); //<- works irrespective of the method being static or instance

            _targetDelegate = (Action<object, object, object>)lambda.Compile();
        }

        public void Invoke(object source, PropertyChangedEventArgs args)
        {
            object t = null;
            if (!_targetRef.TryGetTarget(out t) && _sourceRef.IsAlive) _removeAction.Invoke(this);
            else _targetDelegate.Invoke(t, source, args);
        }

        public static void Remove(System.Delegate sourceDelegate, Delegate target)
        {

        }
    }
}
