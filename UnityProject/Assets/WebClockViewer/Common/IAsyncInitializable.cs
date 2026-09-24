using Cysharp.Threading.Tasks;
using System.Threading;

namespace WebClockViewer
{
    /// <summary>Asynchronous post-construction setup shared by long-lived services.</summary>
    public interface IAsyncInitializable
    {
        /// <summary>
        /// Performs the asynchronous setup. Call once, before the instance is used; the instance is
        /// ready when the returned task completes.
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancels the setup. Implementations abandon it as soon as the token is raised and throw
        /// <see cref="System.OperationCanceledException"/>, leaving the instance half built: the
        /// caller drops it, disposing it first when it implements <see cref="System.IDisposable"/>.
        /// An implementation that keeps background work running past its setup documents how the
        /// token relates to that work.
        /// </param>
        UniTask InitializeAsync(CancellationToken cancellationToken = default);
    }
}
