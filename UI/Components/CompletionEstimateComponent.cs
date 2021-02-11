using LiveSplit.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class CompletionEstimateComponent : IComponent
    {
        protected InfoTextComponent InternalComponent { get; set; }
        public CompletionEstimateSettings Settings { get; set; }
        protected LiveSplitState CurrentState { get; set; }

        protected string ActualComparison { get; set; }
        protected TimingMethod ActualTimingMethod { get; set; }

        protected int LowerBoundSplitIndex { get; set; }
        protected int UpperBoundSplitIndex { get; set; }
        protected bool BoundsValid { get; set; }
        protected bool SplitCompletionsValid { get; set; }
        protected float CurrentProgress { get; set; }

        protected Time FinalSplitTime { get; set; }
        // These inverse variables just speed up computation.
        protected double InverseFinalRealTimeMillis { get; set; }
        protected double InverseFinalGameTimeMillis { get; set; }
        // The fraction of the total run that each split represents.
        protected List<float> SplitCompletions { get; set; }

        public string ComponentName => "Completion Estimate";

        public float HorizontalWidth => InternalComponent.HorizontalWidth;
        public float MinimumHeight => InternalComponent.MinimumHeight;
        public float VerticalHeight => InternalComponent.VerticalHeight;
        public float MinimumWidth => InternalComponent.MinimumWidth;

        public float PaddingTop => InternalComponent.PaddingTop;
        public float PaddingLeft => InternalComponent.PaddingLeft;
        public float PaddingBottom => InternalComponent.PaddingBottom;
        public float PaddingRight => InternalComponent.PaddingRight;

        public IDictionary<string, Action> ContextMenuControls => null;

        public CompletionEstimateComponent(LiveSplitState state)
        {
            Settings = new CompletionEstimateSettings();
            InternalComponent = new InfoTextComponent("Completed", "0%");

            state.OnStart += state_OnStart;
            state.OnSplit += state_OnSplitChange;
            state.OnSkipSplit += state_OnSplitChange;
            state.OnUndoSplit += state_OnSplitChange;
            state.OnSwitchComparisonNext += state_OnSwitchComparison;
            state.OnSwitchComparisonPrevious += state_OnSwitchComparison;
            CurrentState = state;

            BoundsValid = false;
            SplitCompletionsValid = false;
            // Initializing to avoid potential null reference errors.
            ActualComparison = "";
        }

        void state_OnStart(object sender, EventArgs e)
        {
            // Invalidate the underlying data structures, so they are recalculated at the next
            // opportunity.
            SplitCompletionsValid = false;
        }

        void state_OnSplitChange(object sender, EventArgs e)
        {
            // Invalidate the current bounds, so they are recalculated at the next opportunity.
            BoundsValid = false;
        }

        void state_OnSwitchComparison(object sender, EventArgs e)
        {
            // Invalidate the underlying data structures, so they are recalculated at the next
            // opportunity.
            SplitCompletionsValid = false;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);

            InternalComponent.NameLabel.HasShadow
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideCompletionColor ? Settings.CompletionColor : state.LayoutSettings.TextColor;

            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);

            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideCompletionColor ? Settings.CompletionColor : state.LayoutSettings.TextColor;

            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        // Builds the data structures needed to calculate completion estimates. This may be called
        // in the middle of a run, if the comparison or timing method changes. This should only be
        // called while a run is happening.
        void Initialize(LiveSplitState state)
        {
            ActualComparison = GetActualComparison(state);
            ActualTimingMethod = GetActualTimingMethod(state);

            FinalSplitTime = GetSegmentTime(state.Run.Last());
            // If the player has no final split time for the run, we can't make an estimate.
            if (!IsTimeEmpty(FinalSplitTime))
            {
                if (FinalSplitTime.RealTime is TimeSpan pbRealTime)
                {
                    InverseFinalRealTimeMillis = (float)(1.0 / pbRealTime.TotalMilliseconds);
                }
                else
                {
                    InverseFinalRealTimeMillis = 0;
                }
                if (FinalSplitTime.GameTime is TimeSpan pbGameTime)
                {
                    InverseFinalGameTimeMillis = (float)(1.0 / pbGameTime.TotalMilliseconds);
                }
                else
                {
                    InverseFinalGameTimeMillis = 0;
                }

                SplitCompletions = BuildSplitCompletions(state);
            }

            // Validate the data structures, so we don't do this again right away.
            SplitCompletionsValid = true;

            // Invalidate the current bounds, so they are recalculated at the next opportunity.
            BoundsValid = false;
        }

        // Returns the current comparison, taking all settings into consideration.
        string GetActualComparison(LiveSplitState state)
        {
            switch (Settings.Comparison)
            {
                case CompletionEstimateSettings.CompletionComparison.PersonalBest:
                    return "Personal Best";
                case CompletionEstimateSettings.CompletionComparison.BestSegments:
                    return "Best Segments";
                case CompletionEstimateSettings.CompletionComparison.AverageSegments:
                    return "Average Segments";
                default:
                    return state.CurrentComparison;
            }
        }

        // Returns the current timing method, taking all settings into consideration.
        TimingMethod GetActualTimingMethod(LiveSplitState state)
        {
            if (Settings.TimingMethod.Equals(CompletionEstimateSettings.CompletionTimingMethod.RealTime))
            {
                return TimingMethod.RealTime;
            }
            else if (Settings.TimingMethod.Equals(CompletionEstimateSettings.CompletionTimingMethod.GameTime))
            {
                return TimingMethod.GameTime;
            }
            else
            {
                return state.CurrentTimingMethod;
            }
        }

        // Determine if a given split does not have an associated time for the current timing
        // method.
        bool IsTimeEmpty(Time time)
        {
            if (ActualTimingMethod.Equals(TimingMethod.RealTime))
            {
                return time.RealTime == null;
            }
            else
            {
                return time.GameTime == null;
            }
        }

        Time GetZeroTime()
        {
            return new Time(TimeSpan.Zero, TimeSpan.Zero);
        }

        // This is an optimization that removes a lot of division operations, which are costly.
        float DivideTimeByFinalSplit(Time time)
        {
            // Just to prevent crashing. This should not occur.
            if (IsTimeEmpty(FinalSplitTime))
            {
                return 0;
            }

            TimeSpan timeSpan;
            double inversePBMillis;
            if (ActualTimingMethod.Equals(TimingMethod.RealTime))
            {
                timeSpan = time.RealTime ?? TimeSpan.Zero;
                inversePBMillis = InverseFinalRealTimeMillis;
            }
            else
            {
                timeSpan = time.GameTime ?? TimeSpan.Zero;
                inversePBMillis = InverseFinalGameTimeMillis;
            }

            if (timeSpan.Equals(TimeSpan.Zero))
            {
                return 0;
            }

            return (float)(timeSpan.TotalMilliseconds * inversePBMillis);
        }

        // Obtains the time of a given segment, depending on the comparison method. Can return an
        // empty Time.
        Time GetSegmentTime(ISegment segment)
        {
            if ("Personal Best".Equals(ActualComparison))
            {
                return segment.PersonalBestSplitTime;
            }

            return segment.Comparisons.ContainsKey(ActualComparison)
                ? segment.Comparisons[ActualComparison]
                : new Time();
        }

        // Builds a list of completion amounts for each split (representing what fraction of the
        // run is complete when this split begins). If a split was skipped, its completion value is
        // zero.
        List<float> BuildSplitCompletions(LiveSplitState state)
        {
            List<float> completions = new List<float>();

            foreach (var split in state.Run)
            {
                Time splitTime = GetSegmentTime(split);
                float completionAmount = !IsTimeEmpty(splitTime)
                    ? DivideTimeByFinalSplit(splitTime)
                    : 0;
                completions.Add(completionAmount);
            }

            return completions;
        }

        // Search for the split that will act as the lower bound for completion and time. This will
        // return -1 if the lower bound is the very start of the run.
        int GetLowerBoundSplitIndex(LiveSplitState state)
        {
            // If the current index is invalid, return the base case.
            if (state.CurrentSplitIndex <= 0 || state.CurrentSplitIndex >= state.Run.Count())
            {
                return -1;
            }

            // Look backward through the splits for one that has its split time populated and also
            // has a valid split completion. Both are necessary requirements.
            int currentSplitIndex = state.CurrentSplitIndex - 1;
            Time splitStartTime = state.Run[currentSplitIndex].SplitTime;
            float splitCompletion = SplitCompletions[currentSplitIndex];

            while (currentSplitIndex > 0 && (IsTimeEmpty(splitStartTime) || splitCompletion == 0))
            {
                currentSplitIndex -= 1;
                splitStartTime = state.Run[currentSplitIndex].SplitTime;
                splitCompletion = SplitCompletions[currentSplitIndex];
            }

            // If we looked all the way back to the start of the run with no luck, return -1.
            if (currentSplitIndex == 0 && (IsTimeEmpty(splitStartTime) || splitCompletion == 0))
            {
                return -1;
            }

            return currentSplitIndex;
        }

        // Search for the split that will act as the upper bound for completion. This should be
        // guaranteed to find a split, as we don't ever run it unless a PB exists.
        int GetUpperBoundSplitIndex(LiveSplitState state)
        {
            // If the current index is invalid, return the final split.
            if (state.CurrentSplitIndex <= 0 || state.CurrentSplitIndex >= state.Run.Count())
            {
                return state.Run.Count() - 1;
            }

            int currentSplitIndex = state.CurrentSplitIndex;
            float upperBound = SplitCompletions[currentSplitIndex];
            // Loop over the splits, going forward, until we hit a non-skipped split. Because we
            // check to see if the runner has a time on the final split before we attempt this, we
            // are guaranteed to eventually find a populated split. We still have a safety check,
            // though.
            while (upperBound == 0 && currentSplitIndex + 1 < state.Run.Count())
            {
                currentSplitIndex += 1;
                upperBound = SplitCompletions[currentSplitIndex];
            }

            // This is, at most, the index of the last split. Still, let's not take chances.
            return currentSplitIndex < state.Run.Count() ? currentSplitIndex : state.Run.Count() - 1;
        }

        // Get the current completion progress.
        float GetProgress(LiveSplitState state)
        {
            // If the run is over, return 100%.
            if (state.CurrentPhase.Equals(TimerPhase.Ended))
            {
                return 1;
            }

            // If there is no final split time for the run, we can't make an estimate.
            if (IsTimeEmpty(FinalSplitTime))
            {
                return -1;
            }

            // If the run has been reset or isn't started, return 0%.
            if (state.CurrentPhase.Equals(TimerPhase.NotRunning))
            {
                return 0;
            }

            // If the bounds have been invalidated, recalculate them.
            if (!BoundsValid)
            {
                LowerBoundSplitIndex = GetLowerBoundSplitIndex(state);
                UpperBoundSplitIndex = GetUpperBoundSplitIndex(state);
                BoundsValid = true;
            }

            // The completion amount of the previous non-skipped split.
            float lowerCompletionBound = LowerBoundSplitIndex >= 0
                ? SplitCompletions[LowerBoundSplitIndex]
                : 0;
            // The completion amount of the current split, or the next non-skipped split.
            float upperCompletionBound = SplitCompletions[UpperBoundSplitIndex];

            // The split time of the previous non-skipped split.
            Time splitStartTime = LowerBoundSplitIndex >= 0
                ? state.Run[LowerBoundSplitIndex].SplitTime
                : GetZeroTime();
            // How far into the current split we are.
            Time currentSplitElapsedTime = state.CurrentTime - splitStartTime;
            // The incremental addition to the completion amount that the elapsed time gives.
            float elapsedTimeCompletion = DivideTimeByFinalSplit(currentSplitElapsedTime);

            // Return the current estimated completion, but do not go higher than the upper bound.
            return Math.Min(lowerCompletionBound + elapsedTimeCompletion, upperCompletionBound);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            // If the actual comparison or timing method have changed, we'll need to invalidate the
            // data structures.
            if (!GetActualComparison(state).Equals(ActualComparison)
                || !GetActualTimingMethod(state).Equals(ActualTimingMethod))
            {
                SplitCompletionsValid = false;
            }

            // If the underlying data structures have been invalidated by the start of a new run,
            // or the timing method changing, we need to re-initialize.
            if (!SplitCompletionsValid)
            {
                Initialize(state);
            }

            float newProgress = GetProgress(state) * 100;
            if (CurrentProgress != newProgress)
            {
                CurrentProgress = newProgress;
                InternalComponent.LongestString = InternalComponent.InformationName;
                string completionFormat = $"{CurrentProgress:0}%";
                if (Settings.Accuracy.Equals(CompletionEstimateSettings.CompletionAccuracy.OneDecimal))
                {
                    completionFormat = Settings.ShowTrailingZeroes
                        ? $"{CurrentProgress:0.0}%"
                        : $"{CurrentProgress:0.#}%";
                }
                else if (Settings.Accuracy.Equals(CompletionEstimateSettings.CompletionAccuracy.TwoDecimal))
                {
                    completionFormat = Settings.ShowTrailingZeroes
                        ? $"{CurrentProgress:0.00}%"
                        : $"{CurrentProgress:0.##}%";
                }
                InternalComponent.InformationValue = CurrentProgress >= 0 ? completionFormat : "?";
            }

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        public void Dispose()
        {
            CurrentState.OnStart -= state_OnStart;
            CurrentState.OnSplit -= state_OnSplitChange;
            CurrentState.OnSkipSplit -= state_OnSplitChange;
            CurrentState.OnUndoSplit -= state_OnSplitChange;
            CurrentState.OnSwitchComparisonNext -= state_OnSwitchComparison;
            CurrentState.OnSwitchComparisonPrevious -= state_OnSwitchComparison;
        }
    }
}
