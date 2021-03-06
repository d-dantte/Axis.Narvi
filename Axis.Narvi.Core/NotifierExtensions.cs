﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Axis.Luna.Extensions;
using Axis.Narvi.Core.Notify;

using static Axis.Luna.Extensions.ExceptionExtension;

namespace Axis.Narvi.Core
{
    public static class NotifierExtensions
    {

        public static object TrueTarget(this Delegate del)
        {
            if (del.Target is Delegate) return (del.Target as Delegate).TrueTarget();
            else return del.Target;
        }
        public static MethodInfo TrueMethod(this Delegate del)
        {
            if (del.Target is Delegate) return (del.Target as Delegate).TrueMethod();
            else return del.Method;
        }

        public static bool IsWeakCallback<Del, EventArg>(this Delegate @delegate)
        where Del : class => @delegate?.Target is ManagedCallback<Del, EventArg>;

        public static bool IsWeakCallback(this Delegate @delegate)
        {
            var tt = @delegate.TrueTarget()?.GetType();

            if (tt == null) return false;

            else return tt.IsGenericType &&
                        tt.GetGenericTypeDefinition() == typeof(ManagedCallback<,>);
        }

        public static INotificationSubscription NotifyFor(this INotifyPropertyChanged @this,
                                                          string property,
                                                          PropertyChangedEventHandler action)
        => @this.NotifyFor(property.Enumerate(), action);

        public static INotificationSubscription NotifyFor(this INotifyPropertyChanged @this,
                                                          IEnumerable<string> properties,
                                                          PropertyChangedEventHandler action)
        => @this.NotifyFor(properties.Contains, action);

        public static INotificationSubscription NotifyFor(this INotifyPropertyChanged @this,
                                                          Func<string, bool> predicate,
                                                          PropertyChangedEventHandler action)
        {
            ThrowNullArguments(() => predicate, () => action);

            PropertyChangedEventHandler h = (source, args) =>
            {
                if (predicate.Invoke(args.PropertyName))
                    action(source, args);
            };

            var callback = new ManagedCallback<PropertyChangedEventHandler, PropertyChangedEventArgs>(h, false, d => @this.PropertyChanged -= d.Invoke);
            @this.PropertyChanged += callback.Invoke;

            return callback;
        }


        public static INotificationSubscription NotifyForPath<Source>(this Source @this,
                                                                      Expression<Func<Source, object>> propertyAccessPath,
                                                                      Action<object, string, NotifiedEventArgs> action)
        where Source : class, INotifiable
        {
            var _path = propertyAccessPath.ToString().Split('.');
            return @this.NotifyForPath(_path.Skip(1).JoinUsing("."), action);
        }

        public static INotificationSubscription NotifyForPath<Source>(this Source @this,
                                                                      string propertyAccessPath,
                                                                      Action<object, string, NotifiedEventArgs> action)
        where Source : class, INotifiable => new PathSegment(@this, propertyAccessPath, action);


        public static INotificationSubscription NotifyFor(this INotifyCollectionChanged @this,
                                                          NotifyCollectionChangedEventHandler action)
        => @this.NotifyFor(null, action);

        public static INotificationSubscription NotifyFor(this INotifyCollectionChanged @this,
                                                          NotifyCollectionChangedAction? changeType,
                                                          NotifyCollectionChangedEventHandler action)
        {
            ThrowNullArguments(() => action);

            NotifyCollectionChangedEventHandler handler = (x, y) => changeType.PipeIf(ct => ct == null || ct == y.Action, ct => action(x, y));

            var callback = new ManagedCallback<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(handler, false, d => @this.CollectionChanged -= d.Invoke);
            @this.CollectionChanged += callback.Invoke;

            return callback;
        }

        //public static bool IsPropertyAttached(this NotifiableBase target, Expression<Func<object>> property, string prefix = null)
        //{
        //    var lambda = property as LambdaExpression;
        //    if (lambda == null) return false;
        //    else if (lambda.Body is UnaryExpression)
        //    {
        //        var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
        //        if (member == null) return false;
        //        else return target.IsPropertyAttached(member.Member.Name, prefix);
        //    }
        //    else if (lambda.Body is MemberExpression)
        //    {
        //        var member = (lambda.Body as MemberExpression);
        //        return target.IsPropertyAttached(member.Member.Name, prefix);
        //    }
        //    else return false;
        //}
        //public static bool IsPropertyAttached(this NotifiableBase target, string property, string prefix = null)
        //=> new PropertySurrogate(target, prefix).IsPropertyAttached(property); //<-- optimize this method to not need to allocate a new object just to test for property being set

        internal static IEnumerable<string> Notifiables(this PropertyInfo pinfo)
        {
            var ownerType = pinfo.DeclaringType;
            var deps = NotifiableBase._propertyDependency.GetOrAdd(ownerType, (ot) =>
            {
                ///build the dependency
                var typeDependencies = new Dictionary<string, HashSet<string>>();

                ot
                    .GetProperties() //(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)
                    .ToList()
                    .ForEach(p =>
                    {
                        var dep = p.Dependencies();
                        if (dep == null || dep.Count() == 0) return;
                        else dep.ToList().ForEach(t =>
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
            if (notifs == null || notifs.Targets == null || notifs?.Targets?.Count() == 0) return new List<string>();
            var list = new List<string>(notifs.Targets);
            if (notifs.InheritBaseNotifiables)
            {
                var basepinfo = pinfo?.DeclaringType.BaseType?.GetProperty(pinfo.Name);
                list.AddRange(basepinfo.Dependencies()); //if basepinfo is null, an empty list is returned
            }
            return list;
        }
    }
}
