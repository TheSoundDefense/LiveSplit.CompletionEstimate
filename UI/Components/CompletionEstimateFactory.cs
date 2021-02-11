using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components
{
    public class CompletionEstimateFactory : IComponentFactory
    {
        public string ComponentName => "Completion Estimate";

        public string Description => "Displays an estimate of how close the run is to completion.";

        public ComponentCategory Category => ComponentCategory.Information;

        public IComponent Create(LiveSplitState state) => new CompletionEstimateComponent(state);

        public string UpdateName => ComponentName;

        public string XMLURL => "";

        public string UpdateURL => "";

        public Version Version => Version.Parse("1.0.0");
    }
}
