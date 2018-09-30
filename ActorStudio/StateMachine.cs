using System.ComponentModel;

namespace ActorStudio
{
    public class StateMachine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private State _currentState;
        private string _instructions;

        public string Instructions
        {
            get => _instructions;
            set
            {
                _instructions = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Instructions)));
            }
        }

        public State CurrentState
        {
            get => _currentState;
            set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentState)));
                    switch (value)
                    {
                        case State.CheckingSmile:
                            Instructions = "Smile tracking started";
                            break;
                        case State.GameStarted:
                            Instructions = "Starting Game";
                            break;
                        case State.Idle:
                        case State.WaitingBigFace:
                        default:
                            Instructions = null;
                            break;
                    }
                }
            }
        }
    }

    public enum State
    {
        Idle,
        WaitingBigFace,
        CheckingSmile,
        GameStarted
    }
}
