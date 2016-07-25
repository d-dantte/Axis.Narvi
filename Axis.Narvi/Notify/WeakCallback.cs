using Axis.Luna.Extensions;
using Axis.Narvi.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Axis.Luna.Extensions.ExceptionExtensions;
using static Axis.Luna.Extensions.ObjectExtensions;

namespace Axis.Narvi.Notify
{
    public abstract class WeakCallback
    {
        protected Type _targetDelegateType = null;
        protected MethodInfo _targetMethodInfo = null;

        protected WeakReference<object> _targetRef = null;
        protected Action<object, object, object> _targetDelegate = null;

        protected int _hashCode = 0;

        public object GetTarget()
        {
            object t = null;
            return _targetRef.TryGetTarget(out t) ? t : null;
        }

        public static Delegate RemoveWeakCallback(Delegate sourceDelegate, Delegate target)
            => RemoveWeakCallback(sourceDelegate, target.TrueTarget(), target.TrueMethod());

        public static Del RemoveWeakCallback<Del>(Del sourceDelegate, Del targetDelegate)
        where Del : class => RemoveWeakCallback(sourceDelegate as Delegate, targetDelegate as Delegate) as Del;

        public static Delegate RemoveWeakCallback(Delegate sourceDelegate, object targetObject, MethodInfo targetMethod)
            => sourceDelegate.GetInvocationList()
                             .LastOrDefault(d => d.TrueTarget() == targetObject && d.TrueMethod() == targetMethod)
                             .Pipe(d => Delegate.Remove(sourceDelegate, d));


        public override bool Equals(object obj)
        {
            object externaltarget = null,
                   internaltarget = null;
            var wc = obj.As<WeakCallback<EventArgs>>();
            if (wc == null) return false;
            return wc._targetMethodInfo == this._targetMethodInfo &&
                   wc._targetRef.TryGetTarget(out externaltarget) &&
                   _targetRef.TryGetTarget(out internaltarget) &&
                   externaltarget == internaltarget;
        }

        public override int GetHashCode() => ValueHash(5, 13, _hashCode, _targetMethodInfo.GetHashCode());
    }

    /// <summary>
    /// Generic WeakCallback implementation that works for any kind of c# event that follows the event pattern.
    /// <para>
    /// usage is intuitive, as it works the same way for already baked classes as well as new classes that want to take advantage of the API.
    /// </para>
    /// <para>
    /// Already baked classes:
    ///     <para>
    ///     <code>
    ///     var l = new MyListenerObject();
    ///     var c = new Control();
    ///     var wcb = new WeakCallback&lt;InitializedEventArg&gt;(l.Initialized, _wcb => c.Initialized -= _wcb.Invoke);
    ///     c.Initialized += wcb.Invoke;
    ///     c.Initialize -= wcb.Invoke; // or wcb.Unsubscribe();
    ///     </code>
    ///     </para>
    /// </para>
    /// </summary>
    /// <typeparam name="EventArg"></typeparam>
    public sealed class WeakCallback<EventArg>: WeakCallback, INotificationSubscription
    {
        private Action<WeakCallback<EventArg>> _removeAction = null;

        private MethodInfo InvokeInfo => this.GetType().GetMethod(nameof(Invoke));


        public WeakCallback(Delegate callback, Action<WeakCallback<EventArg>> removeAction)
        {
            ThrowNullArguments(() => callback, () => removeAction);

            var IsWeakCallback = callback.IsWeakCallback<EventArg>();

            _targetMethodInfo = !IsWeakCallback ? callback.Method :
                                callback.TrueTarget().As<WeakCallback<EventArg>>()._targetMethodInfo;

            _targetRef = new WeakReference<object>(!IsWeakCallback ? callback.TrueTarget() :
                                                   callback.TrueTarget().As<WeakCallback<EventArg>>().GetTarget());

            _removeAction = removeAction;
            _hashCode = _targetMethodInfo.GetHashCode();
            _targetDelegateType = callback.GetType();

            var argType = typeof(EventArg);

            // (t, s, a) => t.SomeMethod(s, a); or Something(s, a);
            var t = Expression.Parameter(typeof(object), "target");
            var s = Expression.Parameter(typeof(object), "source");
            var a = Expression.Parameter(argType, "args");
            
            var callExp = callback.Method.IsStatic ?
                          Expression.Call(callback.Method, s, Expression.Convert(a, argType)) :
                          Expression.Call(Expression.Convert(t, callback.Target.GetType()), callback.Method, s, Expression.Convert(a, argType));

            var lambda = Expression.Lambda(callExp, t, s, a); //<- works irrespective of the method being static or instance

            _targetDelegate = (Action<object, object, object>)lambda.Compile();
        }

        public void Unsubscribe() => _removeAction.Invoke(this);

        public void Invoke(object source, EventArg args)
        {
            object t = null;
            if (!_targetRef.TryGetTarget(out t)) Unsubscribe();
            else _targetDelegate.Invoke(t, source, args);
        }

    }
}
