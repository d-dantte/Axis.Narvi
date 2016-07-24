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
        public static bool IsWeakCallback<D>(this Delegate @delegate)
        where D : class => @delegate?.Target is WeakCallback<D>;

        public static bool IsWeakCallback(this Delegate @delegate)
        {
            var tt = @delegate.Target?.GetType();

            if (tt == null) return false;

            else return tt.IsGenericType &&
                        tt.GetGenericTypeDefinition() == typeof(WeakCallback<>);
        }

        public static NotifyRegistrar NotifyFor(this INotifyPropertyChanged @this,
                                                     string property,
                                                     PropertyChangedEventHandler action)
            => @this.NotifyFor(property.Enumerate(), action);

        public static NotifyRegistrar NotifyFor(this INotifyPropertyChanged @this,
                                                     IEnumerable<string> properties,
                                                     PropertyChangedEventHandler action)
            => @this.NotifyFor(prop => properties.Contains(prop), action);

        public static NotifyRegistrar NotifyFor(this INotifyPropertyChanged @this,
                                                     Func<string, bool> predicate,
                                                     PropertyChangedEventHandler action)
        {
            ThrowNullArguments(() => predicate, () => action);

            PropertyChangedEventHandler wrapper = (x, y) => { if (predicate(y.PropertyName)) action(x, y); };
            PropertyChangedEventHandler handler = null;
            if (@this is NotifierBase)
                @this.PropertyChanged += handler =
                                          lifetime == CallbackLifetime.SourceDependent ?
                                          wrapper :
                                          ManagedCallback(lifetime, wrapper, gdel => @this.PropertyChanged -= gdel);

            else @this.PropertyChanged += handler = ManagedCallback(lifetime, wrapper, gdel => @this.PropertyChanged -= gdel);

            return new NotifyRegistrar(() => @this.PropertyChanged -= handler);
        }


        public static NotifyRegistrar NotifyForPath<Source>(this Source @this,
                                                             Expression<Func<Source, object>> propertyAccessPath,
                                                             PropertyChangedEventHandler action)
        where Source : class, INotifyPropertyChanged => @this.NotifyForPath(CallbackLifetime.SourceDependent, propertyAccessPath, action);

        public static NotifyRegistrar NotifyForPath<Source>(this Source @this,
                                                             CallbackLifetime lifetime,
                                                             Expression<Func<Source, object>> propertyAccessPath,
                                                             PropertyChangedEventHandler action)
        where Source : class, INotifyPropertyChanged
        {
            var pcn = new PropertyChainNotifier<Source>(@this, propertyAccessPath, (s, e) => action.Invoke(s, e.As<PropertyChangedEventArgs>()));
            return new NotifyRegistrar(() => pcn.StopNotification());
        }


        public static NotifyRegistrar NotifyFor(this INotifyCollectionChanged @this,
                                                     NotifyCollectionChangedEventHandler action)
            => @this.NotifyFor(null, action);

        public static NotifyRegistrar NotifyFor(this INotifyCollectionChanged @this,
                                                     NotifyCollectionChangedAction? changeType,
                                                     NotifyCollectionChangedEventHandler action)
        {
            ThrowNullArguments(() => action);

            NotifyCollectionChangedEventHandler wrapper = (x, y) => changeType.PipeIf(ct => ct == null || ct == y.Action, ct => action(x, y));
            NotifyCollectionChangedEventHandler handler = ManagedCallback(lifetime, wrapper, gdel => @this.CollectionChanged -= gdel);
            @this.CollectionChanged += handler;
            return new NotifyRegistrar(() => @this.CollectionChanged -= handler);
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
