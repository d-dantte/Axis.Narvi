using System.Runtime.CompilerServices;

namespace Axis.Narvi.Core.Notify
{

    /// <summary>
    /// For objects that cannot extend NotifierBase, they can implement INotifyPropertyChanged and marshall calls to their properties, or "Notify(...)" to 
    /// a private instance of this object.
    /// </summary>
    public class DelegatedNotifier : NotifiableBase
    {
        public DelegatedNotifier(INotifiable owner)
        : base(owner)
        {
        }

        new public void Set<V>(ref V value, [CallerMemberName]string property = null) => base.Set(ref value, property);
        new public V Get<V>([CallerMemberName]string property = null) => base.Get<V>(property);
    }
}
