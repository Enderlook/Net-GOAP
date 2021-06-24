namespace Enderlook.GOAP
{
    /// <summary>
    /// Interface that allows pooling of non-consumed action handles.<br/>
    /// This interface can be implemented by a helper type.
    /// </summary>
    /// <typeparam name="TActionHandle">Type of action handle.</typeparam>
    public interface IActionHandlePool<TActionHandle>
    {
        /// <summary>
        /// Gives ownership of the action handle.
        /// </summary>
        /// <param name="value">Action handle to give ownership.</param>
        void Return(TActionHandle value);
    }
}
