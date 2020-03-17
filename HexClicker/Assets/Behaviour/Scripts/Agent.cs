using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HexClicker.Behaviour
{
    public enum StateResult
    {
        Failed,
        Succeeded
    }

    public class Agent : MonoBehaviour
    {
        /// <summary>
        /// Current behaviour graph for the agent.
        /// </summary>
        [SerializeField] private Graph graph;

        private bool stateEnded;
        public Node State { get; private set; }
        public bool Stopped { get; private set; }
        public bool Waiting { get; private set; }
        public StateResult Result { get; private set; }

        //public bool Stopped => paused;
        //public bool Waiting => waiting;
        //public Node State => currentState;
        //public StateResult Result => result;

        /// <summary>
        /// Stops the current state of behaviour, and sets a new behaviour graph for the agent.
        /// Call Restart to start.
        /// </summary>
        public void SetBehaviour(Graph graph)
        {
            Stop();
            this.graph = graph;
        }

        public void Pause()
        {
            if (Stopped)
                return;
            Stopped = true;
            if (State != null)
            {
                State.OnPause(this);
            }
        }

        public void Resume()
        {
            if (!Stopped)
                return;
            Stopped = false;
            if (State != null)
            {
                State.OnResume(this);
            }
        }

        public void Stop()
        {
            Pause();
            State = null;
            stateEnded = false;
        }

        public void Restart()
        {
            Stop();
            Stopped = false;
        }

        /// <summary>
        /// Mark the current state as completed, allowing the agent to move on to the next.
        /// If the passed in state is not the current one, it does nothing.
        /// </summary>
        public void End(Node state, StateResult result)
        {
            if (State == null)
                return;

            if (state != State)
                return;

            stateEnded = true;
            Result = result;
            State.OnEnd(this);
        }

        /// <summary>
        /// Mark the current state as completed, allowing the agent to move on to the next.
        /// </summary>
        public void EndCurrent(StateResult result)
        {
            if (State == null)
                return;

            stateEnded = true;
            Result = result;
            State.OnEnd(this);
        }

        private void Update()
        {
            if (graph == null)
                return;

            if (State == null)
            {
                State = graph.entry;
                stateEnded = false;
                if (State != null)
                {
                    State.OnBegin(this);
                }
            }

            if (State == null)
                return;

            if (Stopped)
                return;

            if (stateEnded)
            {
                Node nextState = State.NextState(this);

                if (nextState == null)
                {
                    switch (State.mode)
                    {
                        case StateMode.Single:
                            Waiting = true;
                            break;

                        case StateMode.Loop:
                            Waiting = false;
                            stateEnded = false;
                            State.OnBegin(this);
                            break;

                        case StateMode.Restart:
                            Waiting = false;
                            State = graph.entry;
                            stateEnded = false;
                            if (State != null)
                            {
                                State.OnBegin(this);
                            }
                            break;
                    }
                }
                else
                {
                    Waiting = false;
                    State = nextState;
                    stateEnded = false;
                    if (State != null)
                    {
                        State.OnBegin(this);
                    }
                }
            }
        }
    }
}
