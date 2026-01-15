using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace EditorConfig
{
    class AddMissingRulesAction(List<Keyword> missingRules, EditorConfigDocument document, ITextView view) : BaseSuggestedAction
    {
        readonly List<Keyword> _missingRules = missingRules;

        public override string DisplayText
        {
            get { return "Add Missing Rules"; }
        }

        public override bool HasActionSets
        {
            get { return true; }
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.AddProperty; }
        }

        public override Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            var list = new List<SuggestedActionSet>();

            var addMissingRulesActionAll = new AddMissingRulesActionAll(_missingRules, document, view);

            List<Keyword> missingRulesDotNet = FindMissingRulesSpecific(Category.DotNet);
            List<Keyword> missingRulesCSharp = FindMissingRulesSpecific(Category.CSharp);
            List<Keyword> missingRulesVB = FindMissingRulesSpecific(Category.VisualBasic);

            AddMissingRulesActionDotNet addMissingRulesActionDotNet = null;
            AddMissingRulesActionCSharp addMissingRulesActionCSharp = null;
            AddMissingRulesActionVB addMissingRulesActionVB = null;
            if (missingRulesDotNet.Count() != 0)
            {
                addMissingRulesActionDotNet = new AddMissingRulesActionDotNet(missingRulesDotNet, document, view);
            }
            if (missingRulesCSharp.Count() != 0)
            {
                addMissingRulesActionCSharp = new AddMissingRulesActionCSharp(missingRulesCSharp, document, view);
            }
            if (missingRulesVB.Count() != 0)
            {
                addMissingRulesActionVB = new AddMissingRulesActionVB(missingRulesVB, document, view);
            }

            list.AddRange(CreateActionSet(addMissingRulesActionAll, addMissingRulesActionDotNet, addMissingRulesActionCSharp, addMissingRulesActionVB));
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(list);
        }

        public IEnumerable<SuggestedActionSet> CreateActionSet(params BaseSuggestedAction[] actions)
        {
            actions = [.. actions.Where(val => val != null)];
            return [new SuggestedActionSet(categoryName: null, actions: actions, title: null, priority: SuggestedActionSetPriority.None, applicableToSpan: null)];
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            // do nothing
        }

        internal static List<Keyword> FindMissingRulesAll(List<string> currentRules)
        {
            var missingRules = new List<Keyword>();
            var missingRuleNames = new List<string>();

            foreach (Keyword keyword in SchemaCatalog.VisibleKeywords)
            {
                string curRule = keyword.Name.ToLower(CultureInfo.InvariantCulture);
                if (!currentRules.Contains(curRule) && !missingRuleNames.Contains(curRule) && !curRule.StartsWith("dotnet_naming") && !curRule.Equals("root") && !curRule.Equals("max_line_length"))
                {
                    missingRules.Add(keyword);
                    missingRuleNames.Add(curRule);
                }
            }

            return missingRules;
        }

        private List<Keyword> FindMissingRulesSpecific(Category language)
        {
            var missingRules = new List<Keyword>();
            foreach (Keyword curRule in _missingRules)
            {
                if (curRule.Category == language)
                {
                    missingRules.Add(curRule);
                }
            }

            return missingRules;
        }
    }
}
