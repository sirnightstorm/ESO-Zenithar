namespace ZenitharClient.Src
{
    public class Service : IObservable<Service>
    {
        public string Name { get; set; }
        public ServiceState State { get; set; }
        public string? Activity { get; set; } = null;

        List<IObserver<Service>> observers;

        public Service(string name)
        {
            Name = name;
            State = ServiceState.Idle;
            observers = new List<IObserver<Service>>();
        }

        public void SetState(ServiceState newState, string? activity = null)
        {
            State = newState;
            Activity = activity;

            string stateText = State switch
            {
                ServiceState.Idle => "Idle",
                ServiceState.Waiting => Activity ?? "Waiting",
                ServiceState.Active => Activity ?? "Active",
                ServiceState.Error => Activity ?? "Error",
                _ => "Unknown"
            };
            LogForm.Log($"{Name}: {stateText}");

            foreach (var observer in observers)
                observer.OnNext(this);
        }

        public IDisposable Subscribe(IObserver<Service> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber(observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<Service>> _observers;
            private IObserver<Service> _observer;

            public Unsubscriber(List<IObserver<Service>> observers, IObserver<Service> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }
    }

    public enum ServiceState
    {
        Idle = 0,
        Waiting = 1,
        Active = 2,
        Error = 3
    }
}
