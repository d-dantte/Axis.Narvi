using Axis.Luna.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using static Axis.Luna.Extensions.ExceptionExtension;
using static Axis.Luna.Extensions.Common;

namespace Axis.Narvi.Core.Notify
{
    public abstract class ManagedCallback
    {
        protected Type _targetDelegateType = null;
        protected MethodInfo _targetMethodInfo = null;

        protected WeakReference<object> _targetWeakRef = null;
        protected object _targetStrongRef = null;

        protected int _hashCode = 0;
        protected bool _isGcCollectible = false;

        public object GetTarget()
        {
            object t = null;
            return !_isGcCollectible ? _targetStrongRef :
                   _targetWeakRef?.TryGetTarget(out t) ?? false ? t : null; //<-- _targetRef will be null if the _targetMethodInfo is a static method
        }

        public static Delegate RemoveWeakCallback(Delegate sourceDelegate, Delegate target)
         => RemoveWeakCallback(sourceDelegate, target.TrueTarget(), target.TrueMethod());

        public static Del RemoveWeakCallback<Del>(Del sourceDelegate, Del targetDelegate)
        where Del : class => RemoveWeakCallback(sourceDelegate as Delegate, targetDelegate as Delegate) as Del;

        public static Delegate RemoveWeakCallback(Delegate sourceDelegate, object targetObject, MethodInfo targetMethod)
        => sourceDelegate
            .GetInvocationList()
            .LastOrDefault(d => d.TrueTarget()?.Equals(targetObject) ?? false && d.TrueMethod() == targetMethod)
            .Pipe(d => Delegate.Remove(sourceDelegate, d));
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
    public sealed class ManagedCallback<CallbackType, EventArg> : ManagedCallback, INotificationSubscription
    where CallbackType : class
    {
        private Action<ManagedCallback<CallbackType, EventArg>> _removeAction = null;

        private MethodInfo InvokeInfo => this.GetType().GetMethod(nameof(Invoke));

        private Action<object, object, EventArg> _targetDelegate = null;



        public ManagedCallback(CallbackType callback, Action<ManagedCallback<CallbackType, EventArg>> removeAction)
        : this(callback, true, removeAction)
        { }

        public ManagedCallback(CallbackType callback, bool isGcCollectible, Action<ManagedCallback<CallbackType, EventArg>> removeAction)
        {
            ThrowNullArguments(() => callback, () => removeAction);

            var callbackDelegate = callback.As<Delegate>().ThrowIfNull("callback MUST be a delegate");
            var isWeakCallback = callbackDelegate.IsWeakCallback<CallbackType, EventArg>();

            _isGcCollectible = !isWeakCallback ? isGcCollectible :
                               callbackDelegate.TrueTarget().As<ManagedCallback<CallbackType, EventArg>>()._isGcCollectible;

            var trueTarget = !isWeakCallback ? callbackDelegate.TrueTarget() :
                             callbackDelegate.TrueTarget().As<ManagedCallback<CallbackType, EventArg>>().GetTarget();

            _targetMethodInfo = !isWeakCallback ? callbackDelegate.Method :
                                callbackDelegate.TrueTarget().As<ManagedCallback<CallbackType, EventArg>>()._targetMethodInfo;

            _targetWeakRef = !_isGcCollectible ? null :
                             _targetMethodInfo.IsStatic ? null :
                             new WeakReference<object>(trueTarget);

            _targetStrongRef = !_isGcCollectible ? trueTarget : null;


            _removeAction = removeAction;
            _hashCode = _targetMethodInfo.GetHashCode();
            _targetDelegateType = typeof(CallbackType);

            var argType = typeof(EventArg);

            // (t, s, a) => t.SomeMethod(s, a); or Something(s, a);
            var t = Expression.Parameter(typeof(object), "target");
            var s = Expression.Parameter(typeof(object), "source");
            var a = Expression.Parameter(argType, "args");
            
            var callExp = _targetMethodInfo.IsStatic ?
                          Expression.Call(_targetMethodInfo, s, Expression.Convert(a, argType)) :
                          Expression.Call(Expression.Convert(t, trueTarget.GetType()), _targetMethodInfo, s, Expression.Convert(a, argType));

            var lambda = Expression.Lambda(callExp, t, s, a); //<- works regardless of the method being static or instance scoped

            _targetDelegate = (Action<object, object, EventArg>)lambda.Compile();
        }

        public void Unsubscribe() => _removeAction.Invoke(this);

        public void Invoke(object source, EventArg args)
        {
            object t = _targetStrongRef;
            if (_isGcCollectible && !_targetWeakRef.TryGetTarget(out t)) Unsubscribe();
            else _targetDelegate.Invoke(t, source, args);
        }
        

        public override bool Equals(object obj)
        {
            object externaltarget = null,
                   internaltarget = null;
            var wc = obj.As<ManagedCallback<CallbackType, EventArg>>();
            if (wc == null) return false;

            wc._targetWeakRef?.TryGetTarget(out externaltarget);
            _targetWeakRef?.TryGetTarget(out internaltarget);

            return wc._targetMethodInfo == this._targetMethodInfo &&
                   wc._isGcCollectible == _isGcCollectible &&
                   wc._targetStrongRef == _targetStrongRef &&
                   externaltarget == internaltarget &&
                   _targetStrongRef == wc._targetStrongRef;
        }

        public override int GetHashCode() => ValueHash(5, 13, _hashCode, _targetMethodInfo.GetHashCode());

    }
}
