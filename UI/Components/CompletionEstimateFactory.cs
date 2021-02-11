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

        public string UpdateURL => "https://raw.githubusercontent.com/TheSoundDefense/LiveSplit.CompletionEstimate/master/";

        public string XMLURL => UpdateURL + "Components/update.LiveSplit.CompletionEstimate.xml";

        public Version Version => Version.Parse("1.0.0");
    }
}
