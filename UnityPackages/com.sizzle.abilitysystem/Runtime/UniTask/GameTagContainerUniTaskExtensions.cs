using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sizzle.GameTagSystem;

namespace Sizzle.GameTagSystem
{
    public static class GameTagContainerUniTaskExtensions
    {
        public static async UniTask WaitUntilHasExactTagAsync(
            this GameTagContainer container,
            GameTag tag,
            CancellationToken cancellationToken = default)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (tag.IsEmpty || container.HasExactTag(tag))
                return;

            while (!container.HasExactTag(tag))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public static UniTask WaitUntilHasExactTagAsync(
            this GameTagContainer container,
            string tag,
            CancellationToken cancellationToken = default)
        {
            return WaitUntilHasExactTagAsync(container, new GameTag(tag), cancellationToken);
        }

        public static async UniTask WaitUntilMissingExactTagAsync(
            this GameTagContainer container,
            GameTag tag,
            CancellationToken cancellationToken = default)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (tag.IsEmpty || !container.HasExactTag(tag))
                return;

            while (container.HasExactTag(tag))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public static UniTask WaitUntilMissingExactTagAsync(
            this GameTagContainer container,
            string tag,
            CancellationToken cancellationToken = default)
        {
            return WaitUntilMissingExactTagAsync(container, new GameTag(tag), cancellationToken);
        }

        public static UniTask WaitUntilTagNotifiedAsync(
            this GameTagContainer container,
            GameTag tag,
            CancellationToken cancellationToken = default)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (tag.IsEmpty)
                return UniTask.CompletedTask;

            if (cancellationToken.IsCancellationRequested)
                return UniTask.FromCanceled(cancellationToken);

            var completionSource = new UniTaskCompletionSource();
            CancellationTokenRegistration registration = default;
            GameTagContainer.GameTagNotifiedHandler handler = null;

            void Cleanup()
            {
                container.OnTagNotified -= handler;
                registration.Dispose();
            }

            handler = notifiedTag =>
            {
                if (!notifiedTag.Equals(tag))
                    return;

                Cleanup();
                completionSource.TrySetResult();
            };

            container.OnTagNotified += handler;

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    Cleanup();
                    completionSource.TrySetCanceled(cancellationToken);
                });
            }

            return completionSource.Task;
        }

        public static UniTask WaitUntilTagNotifiedAsync(
            this GameTagContainer container,
            string tag,
            CancellationToken cancellationToken = default)
        {
            return WaitUntilTagNotifiedAsync(container, new GameTag(tag), cancellationToken);
        }
    }
}