using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HexClicker.Behaviour
{
    public class Agent : MonoBehaviour
    {
        /// <summary>
        /// Current behaviour graph for the agent.
        /// </summary>
        [SerializeField] private Graph graph;

        /// <summary>
        /// Called when a new state begins.
        /// </summary>
        [SerializeField] private UnityEvent onStateBegin;

        /// <summary>
        /// Called when a state reaches an end.
        /// </summary>
        [SerializeField] private UnityEvent onStateEnd;
        [SerializeField] private UnityEvent onPause;
        [SerializeField] private UnityEvent onResume;

        private Node currentState;
        private bool stateComplete;
        private bool paused = true;
        private bool waiting;

        public bool Stopped => paused;
        public bool Waiting => waiting;
        public Node State => currentState;

        public void Awake()
        {
            Restart();
        }

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
            if (paused)
                return;
            paused = true;
            if (currentState != null)
            {
                onPause.Invoke();
                currentState.OnPause(this);
            }
        }
        public void Resume()
        {
            if (!paused)
                return;
            paused = false;
            if (currentState != null)
            {
                onResume.Invoke();
                currentState.OnResume(this);
            }
        }

        public void Stop()
        {
            Pause();
            currentState = null;
            stateComplete = false;
        }

        public void Restart()
        {
            Stop();
            paused = false;
        }

        /// <summary>
        /// Mark the current state as completed, allowing the agent to move on to the next.
        /// If the passed in state is not the current one, it does nothing.
        /// </summary>
        public void Complete(Node state)
        {
            if (currentState == null)
                return;

            if (state != currentState)
                return;

            stateComplete = true;
            onStateEnd.Invoke();
            currentState.OnEnd(this);
        }

        /// <summary>
        /// Mark the current state as completed, allowing the agent to move on to the next.
        /// </summary>
        public void CompleteCurrent()
        {
            if (currentState == null)
                return;

            stateComplete = true;
            onStateEnd.Invoke();
            currentState.OnEnd(this);
        }

        private void Update()
        {
            if (graph == null)
                return;

            if (currentState == null)
            {
                currentState = graph.entry;
                stateComplete = false;
                if (currentState != null)
                {
                    onStateBegin.Invoke();
                    currentState.OnBegin(this);
                }
            }

            if (currentState == null)
                return;

            if (paused)
                return;

            if (stateComplete)
            {
                Node nextState = currentState.Evaluate(this);

                if (nextState == null)
                {
                    switch (currentState.mode)
                    {
                        case StateMode.Single:
                            waiting = true;
                            break;

                        case StateMode.Loop:
                            waiting = false;
                            stateComplete = false;
                            onStateBegin.Invoke();
                            currentState.OnBegin(this);
                            break;

                        case StateMode.Restart:
                            waiting = false;
                            currentState = graph.entry;
                            stateComplete = false;
                            if (currentState != null)
                            {
                                onStateBegin.Invoke();
                                currentState.OnBegin(this);
                            }
                            break;
                    }
                }
                else
                {
                    waiting = false;
                    currentState = nextState;
                    stateComplete = false;
                    if (currentState != null)
                    {
                        onStateBegin.Invoke();
                        currentState.OnBegin(this);
                    }
                }
            }
        }
    }
}
