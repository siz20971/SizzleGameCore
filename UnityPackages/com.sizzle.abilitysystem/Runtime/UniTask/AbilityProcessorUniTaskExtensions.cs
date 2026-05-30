using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sizzle.GameTagSystem;

namespace Sizzle.AbilitySystem
{
    public static class AbilityProcessorUniTaskExtensions
    {
        public static async UniTask<AbilityActivateResult> ActivateAbilityAndWaitAsync(
            this AbilityProcessor processor,
            GameTag gameTag,
            AbilityActivatePayload payload = null,
            CancellationToken cancellationToken = default,
            bool cancelOnCancellation = false)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            AbilityActivateResult result = processor.TryActivateAbility(gameTag, payload);
            if (result != AbilityActivateResult.Success)
                return result;

            try
            {
                await processor.WaitUntilAbilityEndedAsync(gameTag, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (cancelOnCancellation)
                    processor.CancelAbility(gameTag);
                throw;
            }

            return result;
        }

        public static UniTask<AbilityActivateResult> ActivateAbilityAndWaitAsync(
            this AbilityProcessor processor,
            string gameTag,
            AbilityActivatePayload payload = null,
            CancellationToken cancellationToken = default,
            bool cancelOnCancellation = false)
        {
            return ActivateAbilityAndWaitAsync(processor, new GameTag(gameTag), payload, cancellationToken, cancelOnCancellation);
        }

        public static async UniTask WaitUntilAbilityEndedAsync(
            this AbilityProcessor processor,
            GameTag gameTag,
            CancellationToken cancellationToken = default)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            if (gameTag.IsEmpty)
                return;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AbilityRuntimeContext context = processor.GetAbilityContext(gameTag);
                if (context == null || !context.State.IsActive)
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public static UniTask WaitUntilAbilityEndedAsync(
            this AbilityProcessor processor,
            string gameTag,
            CancellationToken cancellationToken = default)
        {
            return WaitUntilAbilityEndedAsync(processor, new GameTag(gameTag), cancellationToken);
        }

        public static async UniTask WaitUntilAbilityActiveAsync(
            this AbilityProcessor processor,
            GameTag gameTag,
            CancellationToken cancellationToken = default)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            if (gameTag.IsEmpty)
                return;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (processor.IsActive(gameTag))
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        public static UniTask WaitUntilAbilityActiveAsync(
            this AbilityProcessor processor,
            string gameTag,
            CancellationToken cancellationToken = default)
        {
            return WaitUntilAbilityActiveAsync(processor, new GameTag(gameTag), cancellationToken);
        }

        public static async UniTask WaitUntilNoAbilityIsActiveAsync(
            this AbilityProcessor processor,
            CancellationToken cancellationToken = default)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            while (processor.AnyAbilityIsActive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}