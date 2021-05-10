using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiegoG.WebWatcher
{
    /// <summary>
    /// An interface defining a class that will watch a specific site for relevant changes and fetch the necessary data.
    /// </summary>
    public interface IWebWatcher
    {
        /// <summary>
        /// The name of the routine
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The interval between checks
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// A regular check
        /// </summary>
        /// <returns></returns>
        public Task Check();

        /// <summary>
        /// A check that is issued once, when the IWebWatcher object is added to the list
        /// </summary>
        /// <returns></returns>
        public Task FirstCheck();

        public bool Equals(object other)
            => ReferenceEquals(this, other);
    }
}
