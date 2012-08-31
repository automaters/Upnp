using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automaters.Core
{
    /// <summary>
    /// Generic EventArgs class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventArgs<T> : EventArgs
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public EventArgs(T value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value
        {
            get;
            set;
        }

    }
}
