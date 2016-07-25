using static Axis.Luna.Extensions.ObjectExtensions;
using static Axis.Luna.Extensions.ExceptionExtensions;
using static Axis.Luna.Extensions.EnumerableExtensions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Axis.Narvi.Notify;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Specialized;
using Axis.Narvi.Extensions;

namespace Axis.Narvi.Extensions
{

    public static class NotifierExtensions
    {

        public static object TrueTarget(this Delegate del)
        {
            if (del.Target.Is<Delegate>()) return (del.Target as Delegate).TrueTarget();
            else return del.Target;
        }
        public static MethodInfo TrueMethod(this Delegate del)
        {
            if (del.Target.Is<Delegate>()) return (del.Target as Delegate).TrueMethod();
            else return del.Method;
        }

        public static bool IsWeakCallback<EventArg>(this Delegate @delegate)
            => @delegate?.Target is WeakCallback<EventArg>;

        public static bool IsWeakCallback(this Delegate @delegate)
        {
            var tt = @delegate.TrueTarget()?.GetType();

            if (tt == null) return false;

            else return tt.IsGenericType &&
                        tt.GetGenericTypeDefinition() == typeof(WeakCallback<>);
        }

        public static INotificationSubscription NotifyFor(this INotifyPropertyChanged @this,
                                                          string property,
                                                          PropertyChangedEventHandler action)
            => @this.NotifyFor(property.Enumerate(), action);

        public static INotificationSubscription NotifyFor(this INotifyPropertyChanged @this,
                                                          IEnumerable<string> properties,
                                                          PropertyChangedEventHandler action)
            => @this.NotifyFor(prop => properties.Contains(prop), action);

        public static INotificationSubscription NotifyFor(this INotifyPropertyChanged @this,
                                                          Func<string, bool> predicate,
                                                          PropertyChangedEventHandler action)
        {
            ThrowNullArguments(() => predicate, () => action);

            PropertyChangedEventHandler h = (source, args) => args.PipeIf(_args => predicate(_args.PropertyName), _args => action(source, _args));

            var callback = new WeakCallback<PropertyChangedEventArgs>(h, d => @this.PropertyChanged -= d.Invoke);
            @this.PropertyChanged += callback.Invoke;

            return callback;
        }

        
        public static INotificationSubscription NotifyForPath<Source>(this Source @this,
                                                                      Expression<Func<Source, object>> propertyAccessPath,
                                                                      PropertyChangedEventHandler action)
        where Source : class, INotifyPropertyChanged => new PropertyChainNotifier<Source>(@this, propertyAccessPath, (s, e) => action.Invoke(s, e.As<PropertyChangedEventArgs>()));


        public static INotificationSubscription NotifyFor(this INotifyCollectionChanged @this,
                                                     NotifyCollectionChangedEventHandler action)
            => @this.NotifyFor(null, action);

        public static INotificationSubscription NotifyFor(this INotifyCollectionChanged @this,
                                                          NotifyCollectionChangedAction? changeType,
                                                          NotifyCollectionChangedEventHandler action)
        {
            ThrowNullArguments(() => action);

            NotifyCollectionChangedEventHandler handler = (x, y) => changeType.PipeIf(ct => ct == null || ct == y.Action, ct => action(x, y));

            var callback = new WeakCallback<NotifyCollectionChangedEventArgs>(handler, d => @this.CollectionChanged -= d.Invoke);
            @this.CollectionChanged += callback.Invoke;

            return callback;
        }

        public static bool IsPropertyAttached(this NotifierBase target, Expression<Func<object>> property, string prefix = null)
        {
            var lambda = property as LambdaExpression;
            if (lambda == null) return false;
            else if (lambda.Body is UnaryExpression)
            {
                var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
                if (member == null) return false;
                else return target.IsPropertyAttached(member.Member.Name, prefix);
            }
            else if (lambda.Body is MemberExpression)
            {
                var member = (lambda.Body as MemberExpression);
                return target.IsPropertyAttached(member.Member.Name, prefix);
            }
            else return false;
        }
        public static bool IsPropertyAttached(this NotifierBase target, string property, string prefix = null)
            => new NotifierBase.PrefixedPropertySurrogate(target, prefix).IsPropertyAttached(property);

        internal static IEnumerable<string> Notifiables(this PropertyInfo pinfo)
        {
            var ownerType = pinfo.DeclaringType;
            var deps = NotifierBase.propertyDependency.GetOrAdd(ownerType, (ot) =>
            {
                ///build the dependency
                var typeDependencies = new Dictionary<string, HashSet<string>>();

                ot.GetProperties() //(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)
                         .ToList()
                         .ForEach(p =>
                         {
                             var dep = p.Dependencies();
                             if (Eval(() => dep.Count()) == 0) return;
                             else
                                 dep.ToList().ForEach(t =>
                                 {
                                     if (!typeDependencies.ContainsKey(t)) typeDependencies[t] = new HashSet<string>();

                                     typeDependencies[t].Add(p.Name);
                                 });
                         });

                return typeDependencies;
            });

            if (!deps.ContainsKey(pinfo.Name)) return new List<string>();
            else return deps[pinfo.Name];
        }
        internal static IEnumerable<string> Dependencies(this PropertyInfo pinfo)
        {
            var notifs = pinfo.GetCustomAttribute<NotifiedByAttribute>();
            if (Eval(() => notifs.targets.Count()) == 0) return new List<string>();
            var list = new List<string>(notifs.targets);
            if (notifs.inheritBaseNotifiables)
            {
                var basepinfo = Eval(() => pinfo.DeclaringType.BaseType.GetProperty(pinfo.Name));
                list.AddRange(basepinfo.Dependencies()); //if basepinfo is null, an empty list is returned
            }
            return list;
        }


    }
}
