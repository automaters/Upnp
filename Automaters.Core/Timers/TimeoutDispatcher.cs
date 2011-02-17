using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Automaters.Core.Timers
{

    /// <summary>
    /// Class used for scheduling actions and repeatable actions
    /// </summary>
    public class TimeoutDispatcher
    {

        #region Constructors

        public TimeoutDispatcher()
        {
            this.TimeoutObjects = new List<TimeoutObject>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified timeout action.
        /// </summary>
        /// <param name="tick">The tick.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public IDisposable Add(Action tick, TimeSpan timeout)
        {
            var timeoutObj = new TimeoutObject(this, tick, timeout, TimeSpan.Zero);
            this.AddInternal(timeoutObj);
            return timeoutObj;
        }

        /// <summary>
        /// Adds the specified repeating timeout action.
        /// </summary>
        /// <param name="tick">The tick.</param>
        /// <param name="interval">The interval.</param>
        /// <returns></returns>
        public IDisposable AddRepeating(Action tick, TimeSpan interval)
        {
            return this.AddRepeating(tick, interval, TimeSpan.Zero);
        }

        /// <summary>
        /// Adds the specified repeating timeout action.
        /// </summary>
        /// <param name="tick">The tick.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="initialDelay">The initial delay.</param>
        /// <returns></returns>
        public IDisposable AddRepeating(Action tick, TimeSpan interval, TimeSpan initialDelay)
        {
            if (initialDelay <= TimeSpan.Zero)
                initialDelay = interval;

            var timeoutObj = new TimeoutObject(this, tick, initialDelay, interval);
            this.AddInternal(timeoutObj);
            return timeoutObj;
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this.ClearInternal();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Adds the action and starts the thread if necessary.
        /// </summary>
        /// <param name="timeoutObj">The timeout obj.</param>
        protected virtual void AddInternal(TimeoutObject timeoutObj)
        {
            lock (this.TimeoutObjects)
            {
                this.TimeoutObjects.Add(timeoutObj);
                if (this.TimeoutObjects.Count == 1)
                    this.StartTimerThread();

                Monitor.PulseAll(this.TimeoutObjects);
            }
        }

        /// <summary>
        /// Removes the action and stops the thread if necessary.
        /// </summary>
        /// <param name="timeoutObj">The timeout obj.</param>
        protected virtual void RemoveInternal(TimeoutObject timeoutObj)
        {
            lock (this.TimeoutObjects)
            {
                this.TimeoutObjects.Remove(timeoutObj);
                if (this.TimeoutObjects.Count == 0)
                    this.StopTimerThread();

                Monitor.PulseAll(this.TimeoutObjects);
            }
        }

        /// <summary>
        /// Clears the actions and stops the thread.
        /// </summary>
        protected virtual void ClearInternal()
        {
            lock (this.TimeoutObjects)
            {
                this.TimeoutObjects.Clear();
                this.StopTimerThread();
                Monitor.PulseAll(this.TimeoutObjects);
            }
        }

        #endregion

        #region Private Methods

        private void StartTimerThread()
        {
            lock (this.TimeoutObjects)
            {
                this.TimerThread = new Thread(this.RunTimerThread);
                this.TimerThread.IsBackground = true;
                this.TimerThread.Start();
            }
        }

        private void StopTimerThread()
        {
            lock (this.TimeoutObjects)
            {
                // Set the timerthread to null which is the key to stop it and then signal it
                this.TimerThread = null;
                Monitor.PulseAll(this.TimeoutObjects);
            }
        }
        
        private void RunTimerThread()
        {
            Thread thread = Thread.CurrentThread;
            TimeoutObject nextTimeout = null;

            while (true)
            {
                lock (this.TimeoutObjects)
                {
                    // The thread is no longer running so break out
                    if (this.TimerThread != thread)
                        break;

                    // If we have no items left then exit the thread
                    if (this.TimeoutObjects.Count == 0)
                        break;

                    // Get our next timeout object
                    nextTimeout = this.TimeoutObjects.Where(obj => !obj.IsExecuting).OrderBy(obj => obj.NextTick).FirstOrDefault();

                    // If we didn't find anything that means all timeouts are executing still
                    // So just wait indefinitely until someone wakes us up
                    if (nextTimeout == null)
                    {
                        Monitor.Wait(this.TimeoutObjects);
                        continue;
                    }

                    // If our next timeout is not ready then wait for it to be
                    if (!nextTimeout.IsElapsed)
                    {
                        // Wait until we get pulsed or our timeout is up then make sure we're still running and try again
                        Monitor.Wait(this.TimeoutObjects, nextTimeout.TimeRemaining);
                        continue;
                    }

                    // If the object we're about to execute is a one time action then just remove it now
                    if (nextTimeout.Interval == TimeSpan.Zero)
                        nextTimeout.Dispose();
                }

                // If we found the next timeout to trigger then trigger it
                if (nextTimeout != null)
                {
                    // Create a local variable to avoid issues with the closure
                    TimeoutObject obj = nextTimeout;
                    obj.IsExecuting = true;
                    ThreadPool.QueueUserWorkItem((data) =>
                    {
                        // Execute the tick and then figure out the next tick time
                        obj.Tick();
                        obj.NextTick = DateTime.Now.Add(obj.Interval);
                        obj.IsExecuting = false;

                        // If this is a repeating job then we should probably notify our thread
                        // That we updated the next tick time so it knows how long to wait
                        if (obj.Interval != TimeSpan.Zero)
                        {
                            lock (this.TimeoutObjects)
                            {
                                // Since some time may have passed make sure we're still valid before signaling
                                if (this.TimeoutObjects.Contains(obj))
                                    Monitor.PulseAll(this.TimeoutObjects);
                            }
                        }
                    });
                }
            }
        }

        #endregion

        #region Properties

        protected Thread TimerThread
        {
            get;
            private set;
        }

        protected List<TimeoutObject> TimeoutObjects
        {
            get;
            private set;
        }

        #endregion
        
        #region Classes

        /// <summary>
        /// Represents a disposable timeout object used internally by the TimeoutDispatcher
        /// </summary>
        protected class TimeoutObject : IDisposable
        {

            public TimeoutObject(TimeoutDispatcher dispatcher, Action tick, TimeSpan delay, TimeSpan interval)
            {
                this.Dispatcher = dispatcher;
                this.Tick = tick;
                this.NextTick = DateTime.Now.Add(delay);
                this.Interval = interval;
            }

            public void Dispose()
            {
                this.Dispatcher.RemoveInternal(this);
            }

            /// <summary>
            /// Gets or sets the dispatcher.
            /// </summary>
            /// <value>
            /// The dispatcher.
            /// </value>
            public TimeoutDispatcher Dispatcher
            {
                get;
                protected set;
            }

            /// <summary>
            /// Gets or sets the tick action.
            /// </summary>
            /// <value>
            /// The tick.
            /// </value>
            public Action Tick
            {
                get;
                protected set;
            }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is executing.
            /// </summary>
            /// <value>
            /// 	<c>true</c> if this instance is executing; otherwise, <c>false</c>.
            /// </value>
            public bool IsExecuting
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the next tick.
            /// </summary>
            /// <value>
            /// The next tick.
            /// </value>
            public DateTime NextTick
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the interval.
            /// </summary>
            /// <value>
            /// The interval.
            /// </value>
            public TimeSpan Interval
            {
                get;
                protected set;
            }

            /// <summary>
            /// Gets a value indicating whether the NextTick has elapsed.
            /// </summary>
            /// <value>
            /// 	<c>true</c> if the NextTick has elapsed; otherwise, <c>false</c>.
            /// </value>
            public bool IsElapsed
            {
                get { return (DateTime.Now >= this.NextTick); }
            }

            /// <summary>
            /// Gets the time remaining.
            /// </summary>
            public TimeSpan TimeRemaining
            {
                get { return this.NextTick.Subtract(DateTime.Now); }
            }

        }

        #endregion

    }

}
