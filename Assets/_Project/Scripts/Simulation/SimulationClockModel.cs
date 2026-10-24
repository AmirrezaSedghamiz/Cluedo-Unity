using System;

namespace TomorrowsCrime.Simulation
{
    /// <summary>
    /// Pure, deterministic logic model for the 5-minute time loop clock.
    /// Completely independent of UnityEngine.MonoBehaviour to enable fast,
    /// robust, millisecond-accurate unit testing in EditMode.
    /// </summary>
    public class SimulationClockModel
    {
        public const float DefaultLoopDurationSeconds = 300f; // 5 minutes

        public float Duration { get; }
        public float ElapsedSeconds { get; private set; }
        public int LoopCount { get; private set; }
        public float TimeScale { get; private set; }
        public bool IsPaused { get; private set; }

        public float RemainingSeconds => Math.Max(0f, Duration - ElapsedSeconds);
        public float NormalizedProgress => Duration > 0f ? Math.Clamp(ElapsedSeconds / Duration, 0f, 1f) : 0f;
        public bool IsLoopFinished => ElapsedSeconds >= Duration;

        public event Action<float, float> OnTick; // (elapsed, remaining)
        public event Action<int> OnLoopCompleted; // loopCount
        public event Action<int> OnLoopStarted;   // loopCount
        public event Action<float> OnTimeScaleChanged;
        public event Action<bool> OnPauseStateChanged;

        public SimulationClockModel(float duration = DefaultLoopDurationSeconds)
        {
            Duration = duration > 0f ? duration : DefaultLoopDurationSeconds;
            TimeScale = 1.0f;
            IsPaused = false;
            LoopCount = 1;
            ElapsedSeconds = 0f;
        }

        public void Tick(float deltaRealtimeSeconds)
        {
            if (IsPaused || deltaRealtimeSeconds <= 0f) return;

            float scaledDelta = deltaRealtimeSeconds * TimeScale;
            ElapsedSeconds += scaledDelta;

            if (ElapsedSeconds >= Duration)
            {
                ElapsedSeconds = Duration;
                OnTick?.Invoke(ElapsedSeconds, RemainingSeconds);
                TriggerLoopTurnover();
            }
            else
            {
                OnTick?.Invoke(ElapsedSeconds, RemainingSeconds);
            }
        }

        public void TriggerLoopTurnover()
        {
            int completedLoop = LoopCount;
            LoopCount++;
            ElapsedSeconds = 0f;
            OnLoopCompleted?.Invoke(completedLoop);
            OnLoopStarted?.Invoke(LoopCount);
            OnTick?.Invoke(ElapsedSeconds, RemainingSeconds);
        }

        public void SetTimeScale(float scale)
        {
            float clampedScale = Math.Clamp(scale, 0.1f, 10f);
            if (Math.Abs(TimeScale - clampedScale) > 0.001f)
            {
                TimeScale = clampedScale;
                OnTimeScaleChanged?.Invoke(TimeScale);
            }
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused != paused)
            {
                IsPaused = paused;
                OnPauseStateChanged?.Invoke(IsPaused);
            }
        }

        public void TogglePause() => SetPaused(!IsPaused);

        public void ResetToBeginning()
        {
            ElapsedSeconds = 0f;
            OnTick?.Invoke(ElapsedSeconds, RemainingSeconds);
        }

        public string GetFormattedTime()
        {
            int minutes = (int)(ElapsedSeconds / 60f);
            int seconds = (int)(ElapsedSeconds % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }

        public string GetFormattedRemainingTime()
        {
            int minutes = (int)(RemainingSeconds / 60f);
            int seconds = (int)(RemainingSeconds % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
