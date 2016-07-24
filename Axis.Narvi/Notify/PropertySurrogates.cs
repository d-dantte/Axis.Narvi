using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axis.Narvi.Notify
{

    /// <summary>
    /// Used for setting prefixed-attached properties on the <c>NotifierBase</c>.
    /// Attached properties are properties that do not naturally occur on a "Notifiable" object.
    /// If a key is specified, the key's hashcode is used to generate a unique property name for the property.
    /// This makes it further possible to have multiple properties set with the same base property name, but
    /// differentiated by the keys used.
    /// </summary>
    public class PrefixedPropertySurrogate : IPropertySurrogate
    {
        #region Statics
        private static readonly string DefaultPrefix = "_____PrefixedProperty_";
        #endregion

        #region init
        public PrefixedPropertySurrogate(NotifierBase targetObject, string prefix = null)
        {
            ThrowNullArguments(() => targetObject);

            this.Prefix = prefix?.Trim().ThrowIf(pfx => string.Empty.Equals(pfx), "Invalid Prefix") ?? DefaultPrefix;
            this.Target = targetObject;
        }
        #endregion

        #region properties
        public string Prefix { get; set; }
        public NotifierBase Target { get; private set; }
        #endregion

        #region methods
        public string ResolvePropertyName(string property)
            => $"{Prefix}_{property.ThrowIf(p => string.IsNullOrWhiteSpace(p), "Invalid Property Name").Trim()}";
        public void Set(string unresolvedPropertyName, object value) => Target.set(ref value, ResolvePropertyName(unresolvedPropertyName));
        public V Get<V>(string unresolvedPropertyName) => Target.get<V>(ResolvePropertyName(unresolvedPropertyName)).As<V>();

        public void Set(Expression<Func<object>> exp, object value)
        {
            var lambda = exp as LambdaExpression;
            if (lambda == null) return;
            else if (lambda.Body is UnaryExpression)
            {
                var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
                if (member == null) return;
                else Set(member.Member.Name, value);
            }
            else if (lambda.Body is MemberExpression)
            {
                var member = (lambda.Body as MemberExpression);
                Set(member.Member.Name, value);
            }
        }
        public V Get<V>(Expression<Func<object>> exp)
        {
            var lambda = exp as LambdaExpression;
            if (lambda == null) return default(V);
            else if (lambda.Body is UnaryExpression)
            {
                var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
                if (member == null) return default(V);
                else return Get<V>(member.Member.Name);
            }
            else if (lambda.Body is MemberExpression)
            {
                var member = (lambda.Body as MemberExpression);
                return Get<V>(member.Member.Name);
            }
            else return default(V);
        }

        public bool IsPropertyAttached(string unresolvedPropertyName) => Target.IsSet(ResolvePropertyName(unresolvedPropertyName));
        #endregion
    }

    /// <summary>
    /// This Class does not prefix it's property names; the implication of this is that properties set via this class may clash with 
    /// naturally occuring properties on the target itself - which is the desired effect of this
    /// </summary>
    public class DelegatePropertySurrogate : IPropertySurrogate
    {
        #region init
        public DelegatePropertySurrogate(NotifierBase targetObject)
        {
            ThrowNullArguments(() => targetObject);

            Target = targetObject;
        }
        #endregion

        #region properties
        public NotifierBase Target { get; private set; }
        public IEnumerable<string> Properties => Target.Availableproperties ?? new string[0];
        #endregion

        #region methods
        public string ResolvePropertyName(string property)
            => property.ThrowIf(p => string.IsNullOrWhiteSpace(p), "Invalid Property Name").Trim();
        public void Set(string unresolvedPropertyName, object value) => Target.set(ref value, ResolvePropertyName(unresolvedPropertyName));
        public V Get<V>(string unresolvedPropertyName) => Target.get<V>(ResolvePropertyName(unresolvedPropertyName)).As<V>();

        public void Set(Expression<Func<object>> exp, object value)
        {
            var lambda = exp as LambdaExpression;
            if (lambda == null) return;
            else if (lambda.Body is UnaryExpression)
            {
                var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
                if (member == null) return;
                else Set(member.Member.Name, value);
            }
            else if (lambda.Body is MemberExpression)
            {
                var member = (lambda.Body as MemberExpression);
                Set(member.Member.Name, value);
            }
        }
        public V Get<V>(Expression<Func<object>> exp)
        {
            var lambda = exp as LambdaExpression;
            if (lambda == null) return default(V);
            else if (lambda.Body is UnaryExpression)
            {
                var member = (lambda.Body as UnaryExpression).Operand as MemberExpression;
                if (member == null) return default(V);
                else return Get<V>(member.Member.Name);
            }
            else if (lambda.Body is MemberExpression)
            {
                var member = (lambda.Body as MemberExpression);
                return Get<V>(member.Member.Name);
            }
            else return default(V);
        }

        public bool IsPropertyAttached(string unresolvedPropertyName) => Target.IsSet(ResolvePropertyName(unresolvedPropertyName));

        public void Notify(string unresolvedName) => Target.notify(ResolvePropertyName(unresolvedName));
        #endregion
    }
}
