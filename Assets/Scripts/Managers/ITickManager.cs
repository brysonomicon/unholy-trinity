using System;
using System.Collections;
namespace UnholyTrinity.Managers.Interfaces
{
    public interface ITickManager<out T, in TU> where T : Enum
    {
        /// <summary>
        /// How long this particular action takes to complete
        /// </summary>
        public float ActionDuration { get; }

        /// <summary>
        /// The SAM type of this action, see <see cref="SAMType"/> for more.
        /// </summary>
        public T SAMType { get; }

        /// <summary>
        /// Why this whole thing works.
        /// </summary>
        /// <param name="arg">A generic type that handles all the data
        /// required to make this function run.</param>
        /// <returns>An IEnumerator. Yield it or something.</returns>
        public IEnumerator HandleTick(TU arg);
    }
}