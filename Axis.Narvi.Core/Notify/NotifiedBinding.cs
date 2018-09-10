using Axis.Luna.Extensions;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace Axis.Narvi.Core.Notify
{

    public class BindingProfile
    {
        public INotifiable Notifiable { get; private set; }
        public PropertyInfo Property { get; private set; }

        public BindingProfile(INotifiable obj, string prop)
        {
            if (obj == null || prop == null) throw new ArgumentNullException();

            Notifiable = obj;
            Property = obj.Property(prop);
        }


        public void Set(object value)
        => Property.GetSetMethod().CallAction(Notifiable, value);

        public object Get()
        => Property.GetGetMethod().CallFunc(Notifiable);
    }


    public class NotifiedBinding
    {
        private static AsyncLocal<string> _AsyncLocalGuard = new AsyncLocal<string>();
        private static readonly string CallContextTag = "$__NBindable__Axis.Narvi.NotifiedBinding";

        public enum Mode { TwoWay, LeftToRight, RightToLeft }

        public NotifiedBinding(BindingProfile left, BindingProfile right, Mode mode = Mode.TwoWay)
        {
            //validate the property to be bound
            if (left.Property.PropertyType != right.Property.PropertyType) throw new ArgumentException();

            this.BindingMode = mode;

            this.Left = left;
            if (mode == Mode.TwoWay || mode == Mode.LeftToRight)
                _leftSubscription = Left.Notifiable.NotifyFor(Left.Property.Name, LeftChanged);

            this.Right = right;
            if (mode == Mode.TwoWay || mode == Mode.RightToLeft)
                _rightSubscription = Right.Notifiable.NotifyFor(Right.Property.Name, RightChanged);
        }

        #region Left
        private INotificationSubscription _leftSubscription;
        public BindingProfile Left { get; private set; }
        private void LeftChanged(object sender, PropertyChangedEventArgs args)
        {
            var data = _AsyncLocalGuard.Value;
            if (data != null) return;

            //set the call context guard
            _AsyncLocalGuard.Value = CallContextTag;

            //set the value on the right hand side
            Right.Set(Left.Get());

            //remove the call context guard
            _AsyncLocalGuard.Value = null;
        }
        #endregion

        #region Right
        private INotificationSubscription _rightSubscription;
        public BindingProfile Right { get; private set; }
        private void RightChanged(object sender, PropertyChangedEventArgs args)
        {
            var data = _AsyncLocalGuard.Value;
            if (data != null) return;

            //set the call context guard
            _AsyncLocalGuard.Value = CallContextTag;

            //set the value on the right hand side
            Left.Set(Right.Get());

            //remove the call context guard
            _AsyncLocalGuard.Value = null;
        }
        #endregion

        public Mode BindingMode { get; private set; }

        public void Release()
        {
            if (Left?.Notifiable != null)
            {
                _leftSubscription.Unsubscribe();
                _leftSubscription = null;
            }

            if(Right?.Notifiable != null)
            {
                _rightSubscription.Unsubscribe();
                _rightSubscription = null;
            }

            Left = null;
            Right = null;
        }

    }
}
