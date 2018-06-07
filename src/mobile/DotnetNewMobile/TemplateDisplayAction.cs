using System;
using System.Collections.Generic;

namespace DotnetNewMobile
{
    public class TemplateDisplayAction
    {
        public TemplateDisplayAction(string actionString, Action action, bool isEnabled)
        {
            ActionString = actionString;
            Action = action;
            IsEnabled = isEnabled;
        }

        public string ActionString
        {
            get; set;
        }
        public Action Action
        {
            get; set;
        }
        public bool IsEnabled
        {
            get; set;
        }
        public static string[] GetActionStrings(IEnumerable<TemplateDisplayAction> actions)
        {
            List<string> actStrings = new List<string>();
            foreach (var act in actions)
            {
                if (act.IsEnabled)
                {
                    actStrings.Add(act.ActionString);
                }
            }
            return actStrings.ToArray();
        }

        public static void ExecuteAction(string name, IEnumerable<TemplateDisplayAction> actions)
        {
            foreach (var action in actions)
            {
                if (string.Compare(name, action.ActionString, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    action.Action();
                    break;
                }
            }
        }
    }
}
