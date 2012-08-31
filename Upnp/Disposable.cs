using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automaters.Core
{
    /// <summary>
    /// Class for performing various operations in a using block
    /// </summary>
    public class Disposable : IDisposable
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Disposable"/> class.
        /// </summary>
        /// <param name="disposeAction">The dispose action.</param>
        public Disposable(Action disposeAction)
            : this(null, disposeAction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Disposable"/> class.
        /// </summary>
        /// <param name="startAction">The start action.</param>
        /// <param name="disposeAction">The dispose action.</param>
        public Disposable(Action startAction, Action disposeAction)
        {
            this.StartAction = startAction;
            this.DisposeAction = disposeAction;

            if (this.StartAction != null)
                this.StartAction();
        }

        /// <summary>
        /// Gets or sets the start action.
        /// </summary>
        /// <value>
        /// The start action.
        /// </value>
        public Action StartAction
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the dispose action.
        /// </summary>
        /// <value>
        /// The dispose action.
        /// </value>
        public Action DisposeAction
        {
            get;
            protected set;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var action = this.DisposeAction;
            if (action != null)
                action();
        }
    }
}
