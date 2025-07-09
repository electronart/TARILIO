using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.AI
{
    public enum PerplexityModel
    {
        [Description("sonar")]
        Sonar,
        [Description("sonar-pro")]
        SonarPro,
        [Description("sonar-reasoning")]
        SonarReasoning,
        [Description("sonar-reasoning-pro")]
        SonarReasoningPro,
        // DEPRECATED Models below
        [Description("llama-3.1-sonar-small-128k-online")]
        [Obsolete("Discontinued")]
        Small,
        [Description("llama-3.1-sonar-large-128k-online")]
        [Obsolete("Discontinued")]
        Large,
        [Description("llama-3.1-sonar-huge-128k-online")]
        [Obsolete("Discontinued")]
        Huge
    }
}
