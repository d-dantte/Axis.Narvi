using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Axis.Narvi.Core.Notify
{

    public class NotifiedEventArgs : PropertyChangedEventArgs
    {
        public NotifiedEventArgs(string property, object newValue, object oldValue, ISet<string> nset = null)
        : base(property)
        {
            this.NewValue = newValue;
            this.OldValue = oldValue;
            NotifiedCollection = nset ?? new HashSet<string>();
        }

        public object NewValue { get; private set; }
        public object OldValue { get; private set; }

        /// <summary>
        /// Signifies if the <c>PropertyName</c> represents an initial property-change event in a chain of property-change event.
        /// Meaning, changes to some properties may cause other properties to be notified, this tells if this is the initial property
        /// setting off the chain.
        /// </summary>
        public bool IsAntecedent => NotifiedCollection?.Count == 0 || NotifiedCollection?.First() == PropertyName;

        internal ISet<string> NotifiedCollection { get; set; }
    }
}
