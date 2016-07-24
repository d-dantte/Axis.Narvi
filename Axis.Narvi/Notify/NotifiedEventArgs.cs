using static Axis.Luna.Extensions.ObjectExtensions;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Axis.Narvi.Notify
{

    public class NotifiedEventArgs : PropertyChangedEventArgs
    {
        public NotifiedEventArgs(string property, object newValue, object oldValue, ISet<string> nset = null)
        : base(property)
        {
            this.newValue = newValue;
            this.oldValue = oldValue;
            NotifiedCollection = nset ?? new HashSet<string>();
        }

        public object newValue { get; private set; }
        public object oldValue { get; private set; }

        /// <summary>
        /// Signifies if the <c>PropertyName</c> represents an initial property-change event in a chain of property-change event.
        /// Meaning, changes to some properties may cause other properties to be notified, this tells if this is the initial property
        /// setting off the chain.
        /// </summary>
        public bool IsAntecedent => Eval(() => NotifiedCollection.Count == 0 || NotifiedCollection.First() == PropertyName);

        internal ISet<string> NotifiedCollection { get; set; }
    }
}
