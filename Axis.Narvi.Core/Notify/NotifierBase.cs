using static Axis.Luna.Extensions.TypeExtensions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Axis.Luna.Extensions;

namespace Axis.Narvi.Core.Notify
{

    public abstract class NotifiableBase : INotifiable
    {
        #region INotifyPropertyChanged Members
        private PropertyChangedEventHandler _propChanged; //added
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propChanged += new ManagedCallback<PropertyChangedEventHandler, PropertyChangedEventArgs>(value, d => _propChanged -= d.Invoke).Invoke;
            remove => _propChanged =  ManagedCallback.RemoveWeakCallback(_propChanged, value);
        }
        #endregion

        #region Properties
        internal IEnumerable<string> Availableproperties => _values.Keys;
        private INotifiable _target;
        #endregion

        #region Fields
        private Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();
        private Dictionary<string, object> _values = new Dictionary<string, object>();
        private Dictionary<string, object> _oldvalues = new Dictionary<string, object>();
        internal static ConcurrentDictionary<Type, Dictionary<string, HashSet<string>>> _propertyDependency = new ConcurrentDictionary<Type, Dictionary<string, HashSet<string>>>();
        #endregion

        #region init
        protected NotifiableBase(INotifiable owner = null)
        {
            _target = owner ?? this;
        }
        #endregion

        #region Handlers
        protected virtual void OnPropertyChanged(object sender, NotifiedEventArgs ne)
        {
            if (ne.NotifiedCollection.Contains(ne.PropertyName)) return;
            else ne.NotifiedCollection.Add(ne.PropertyName);

            _propChanged?.Invoke(this._target, ne);
            this.NotifyTargets(ne.PropertyName, ne.NotifiedCollection);
        }
        #endregion

        #region Utils
        public void Notify(Expression<Func<object>> exp)
        {
            if (!(exp is LambdaExpression lambda)) return;

            else if (lambda.Body is UnaryExpression)
            {
                var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
                if (member == null) return;

                else Notify(member.Member.Name);
            }
            else if (lambda.Body is MemberExpression)
            {
                var member = (lambda.Body as MemberExpression);
                Notify(member.Member.Name);
            }
        }
        public void Notify([CallerMemberName] string propertyName = null)
        {
            Notify(propertyName, new HashSet<string>());
        }
        private void Notify(string propertyName, ISet<string> notified)
        {
            var ne = new NotifiedEventArgs(propertyName, 
                                           _target.TryGetPropertyValue(propertyName, out object newValue)? newValue: Get<object>(propertyName), 
                                           GetOld(propertyName), notified);
            OnPropertyChanged(_target, ne);
        }
        private void NotifyTargets(string pname, ISet<string> notified = null)
        {
            var prop = _properties.GetOrAdd(pname, _pname =>
            {
                var type = _target.GetType();
                return type.GetProperty(pname);
            });
            if (prop == null) return;

            var notifies = prop.Notifiables();
            notifies.ForAll(pn => Notify(pn, notified));
        }

        internal protected V Get<V>([CallerMemberName] string property = null)
        {
            var found = _values.TryGetValue(property, out object value);
            if (found)
            {
                if (value == null) return default(V);
                else return (V)value;
            }
            else return default(V);
        }
        internal protected void Set<V>(ref V value, [CallerMemberName] string property = null)
        {
            if (!_values.ContainsKey(property))
            {
                _oldvalues[property] = null;
                _values[property] = value;
                Notify(property);
            }

            //only modify if the old and new values are different
            else if (!EqualityComparer<V>.Default.Equals(value, (V)_values[property]))
            {
                _oldvalues[property] = _values[property];
                _values[property] = value;
                Notify(property);
            }
        }


        private object GetOld(string property)
        {
            if (!_oldvalues.ContainsKey(property)) return null;
            else return _oldvalues[property];
        }
        internal protected bool IsSet([CallerMemberName] string property = null) => _values.ContainsKey(property ?? "");
        #endregion
        
    }
}
