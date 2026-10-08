// <copyright file="ValueSubject.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

namespace Tlumach.Avalonia
{
    /// <summary>
    /// An observable string value that sends its current value to each new subscriber and every new value to all subscribers.
    /// <para>This is a minimal replacement for the BehaviorSubject class of System.Reactive, which Tlumach.Avalonia does not depend on,
    /// because neither Avalonia nor the Tlumach package brings that assembly to an application.</para>
    /// </summary>
    internal sealed class ValueSubject : IObservable<string>, IDisposable
    {
        private readonly Lock _lock = new();

        private IObserver<string>[] _observers = [];

        private string _value;

        private bool _disposed;

        public ValueSubject(string value)
        {
            _value = value;
        }

        public string Value
        {
            get
            {
                lock (_lock)
                    return _value;
            }
        }

        public void OnNext(string value)
        {
            IObserver<string>[] observers;
            lock (_lock)
            {
                if (_disposed)
                    return;
                _value = value;
                observers = _observers;
            }

            foreach (IObserver<string> observer in observers)
                observer.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            // The current value is sent under the lock, so that a value set concurrently by OnNext cannot reach the observer before this one.
            lock (_lock)
            {
                if (_disposed)
                    return Subscription.Empty;
                _observers = [.. _observers, observer];
                observer.OnNext(_value);
            }

            return new Subscription(this, observer);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _observers = [];
            }
        }

        private void Unsubscribe(IObserver<string> observer)
        {
            lock (_lock)
            {
                int index = Array.IndexOf(_observers, observer);
                if (index < 0)
                    return;
                IObserver<string>[] observers = new IObserver<string>[_observers.Length - 1];
                Array.Copy(_observers, 0, observers, 0, index);
                Array.Copy(_observers, index + 1, observers, index, observers.Length - index);
                _observers = observers;
            }
        }

        private sealed class Subscription : IDisposable
        {
            public static readonly Subscription Empty = new(null, null);

            private readonly IObserver<string>? _observer;

            private ValueSubject? _subject;

            public Subscription(ValueSubject? subject, IObserver<string>? observer)
            {
                _subject = subject;
                _observer = observer;
            }

            public void Dispose()
            {
                ValueSubject? subject = Interlocked.Exchange(ref _subject, null);
                if (subject is not null && _observer is not null)
                    subject.Unsubscribe(_observer);
            }
        }
    }
}
