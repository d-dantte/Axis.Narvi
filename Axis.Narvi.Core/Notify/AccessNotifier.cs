using System;
using System.Linq;
using System.Text.RegularExpressions;
using Axis.Luna.Extensions;

using static Axis.Luna.Extensions.ExceptionExtension;

namespace Axis.Narvi.Core.Notify
{
    public class PathSegment: INotificationSubscription
    {
        private static readonly Regex PropertyNamePattern = new Regex(@"^@?[a-zA-Z_]\w*(\.@?[a-zA-Z_]\w*)*$");

        private PathSegment _next;
        private INotifiable _source;
        private Type _sourceType;
        private INotificationSubscription _subscription;
        private Action<object, string, NotifiedEventArgs> _callback;
        
        public string CurrentSegment { get; private set; }
        public string Path { get; private set; }
        public string ParentPath { get; private set; }

        public PathSegment(INotifiable notifiable, string propertyPath, Action<object, string, NotifiedEventArgs> callback)
        : this(notifiable.GetType(), null, propertyPath, callback)
        {
            if (notifiable == null)
                throw new ArgumentException($"Invalid argument: {nameof(notifiable)}");

            //hookup notification for available objects
            HookupCallback(notifiable);
        }

        private PathSegment(Type notifiableType,string parentPath, string propertyPath, Action<object, string, NotifiedEventArgs> callback)
        {
            _sourceType = notifiableType;
            _callback = callback ?? throw new ArgumentException($"Invalid argument: {nameof(callback)}");

            Path = propertyPath;
            var paths = propertyPath
                .Split('.')
                .ThrowIf(_p => !_p.ExactlyAll(PropertyNamePattern.IsMatch), "Invalid Property Name");

            CurrentSegment = paths.First();
            ParentPath = parentPath;

            if (!IsSegmentNotifiable())
                throw new Exception("Segment path not notifiable");

            else if (paths.Length > 1)
            {
                var _nextPath = paths.Skip(1).JoinUsing(".");
                var _nextNotifiableType = SegmentPropertyType();

                _next = new PathSegment(_nextNotifiableType, RelativePath(), _nextPath, callback);
            }
        }
               
        public void Unsubscribe()
        {
            _subscription?.Unsubscribe();
            _next?.Unsubscribe();
            _subscription = null;
        }

        private Type SegmentPropertyType() => _sourceType?.GetProperty(CurrentSegment).PropertyType;
        private bool IsSegmentNotifiable() => _sourceType?.Implements(typeof(INotifiable)) ?? false;
        private string RelativePath() => $"{ParentPath}.{CurrentSegment}".TrimStart(".");
        private void HookupCallback(INotifiable notifiable)
        {
            _source = notifiable;
            _subscription = notifiable.NotifyFor(CurrentSegment, (x, y) =>
            {
                //value has changed, so unsubscribe from old notifiable
                if (_next != null) _next.Unsubscribe();

                //hookup the next segment
                var ne = (NotifiedEventArgs)y;
                if (_next != null && ne.NewValue != null) //<-- if there is a "_next", then this segment MUST be notifiable
                    _next.HookupCallback((INotifiable)ne.NewValue);

                //invoke the callback
                _callback.Invoke(x, RelativePath(), ne);
            });

            INotifiable child = null;
            if (_next != null  && (child = _source.PropertyValue(CurrentSegment) as INotifiable) != null)
                _next.HookupCallback(child);
        }
    }
}
