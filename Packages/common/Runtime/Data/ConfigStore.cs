using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yu5h1Lib
{
    /// <summary>Coordinates one settings scope. Supply a detached copy for mutable values and call from the owning Unity thread.</summary>
    public sealed class ConfigStore<T> : IDisposable
    {
        public enum Status { Idle, Loading, LoadError, Saving, SaveError, Applying, ApplyError, Ready, Disposed }

        private readonly Func<T, T> copy;
        private readonly Action<T> validate;
        private readonly Func<CancellationToken, Task<(bool Found, T Value)>> read;
        private readonly Func<T, CancellationToken, Task> write;
        private readonly Func<T, CancellationToken, Task> apply;
        private readonly T defaults;
        private T accepted;
        private CancellationTokenSource operation;
        private Action changed;

        public Status CurrentStatus { get; private set; }
        public Exception Error { get; private set; }
        public bool IsBusy => CurrentStatus is Status.Loading or Status.Saving or Status.Applying;
        public T Accepted => copy(accepted);
        public T Defaults => copy(defaults);
        public event Action Changed
        {
            add
            {
                if (CurrentStatus == Status.Disposed)
                    throw new ObjectDisposedException(nameof(ConfigStore<T>));
                changed += value;
            }
            remove => changed -= value;
        }

        public ConfigStore(T defaults, Func<T, T> copy, Action<T> validate,
            Func<CancellationToken, Task<(bool Found, T Value)>> read,
            Func<T, CancellationToken, Task> write, Func<T, CancellationToken, Task> apply)
        {
            this.copy = copy ?? throw new ArgumentNullException(nameof(copy));
            this.validate = validate ?? throw new ArgumentNullException(nameof(validate));
            this.read = read ?? throw new ArgumentNullException(nameof(read));
            this.write = write ?? throw new ArgumentNullException(nameof(write));
            this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
            this.defaults = CopyValidated(defaults);
            accepted = this.defaults;
        }

        public Task<bool> LoadAsync() => RunAsync(Status.Loading, Status.LoadError, async token =>
        {
            var result = await read(token);
            token.ThrowIfCancellationRequested();
            return result.Found ? CopyValidated(result.Value) : defaults;
        });

        public Task<bool> SaveAsync(T draft)
        {
            if (CurrentStatus != Status.Ready && CurrentStatus != Status.SaveError)
                return Task.FromResult(false);
            return RunAsync(Status.Saving, Status.SaveError, async token =>
            {
                var value = CopyValidated(draft);
                await write(copy(value), token);
                return value;
            });
        }

        public Task<bool> RetryApplyAsync() => CurrentStatus == Status.ApplyError
            ? RunAsync(Status.Applying, Status.ApplyError, null)
            : Task.FromResult(false);

        public void Dispose()
        {
            if (CurrentStatus == Status.Disposed)
                return;
            CurrentStatus = Status.Disposed;
            try { operation?.Cancel(); }
            catch (AggregateException error) { UnityEngine.Debug.LogException(error); }
            Publish(Status.Disposed);
            changed = null;
        }

        private T CopyValidated(T value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            var snapshot = copy(value);
            validate(copy(snapshot));
            return snapshot;
        }

        private async Task<bool> RunAsync(Status initial, Status failure, Func<CancellationToken, Task<T>> commit)
        {
            if (operation != null || CurrentStatus == Status.Disposed)
                return false;
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            operation = cancellation;
            try
            {
                Publish(initial);
                token.ThrowIfCancellationRequested();
                if (commit != null)
                {
                    var value = await commit(token);
                    token.ThrowIfCancellationRequested();
                    accepted = value;
                    failure = Status.ApplyError;
                    Publish(Status.Applying);
                    token.ThrowIfCancellationRequested();
                }
                await apply(copy(accepted), token);
                token.ThrowIfCancellationRequested();
                Publish(Status.Ready);
                return CurrentStatus != Status.Disposed;
            }
            catch (Exception error)
            {
                if (CurrentStatus != Status.Disposed)
                    Publish(failure, error);
                return false;
            }
            finally
            {
                operation = null;
            }
        }

        private void Publish(Status status, Exception error = null)
        {
            CurrentStatus = status;
            Error = error;
            var listeners = changed;
            if (listeners == null)
                return;
            foreach (Action listener in listeners.GetInvocationList())
            {
                if (CurrentStatus != status)
                    return;
                try { listener(); }
                catch (Exception observerError) { UnityEngine.Debug.LogException(observerError); }
            }
        }
    }
}
