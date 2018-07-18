using System;
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
    class AddMissingRulesAction : BaseSuggestedAction
    {
        List<Keyword> _missingRules;
        EditorConfigDocument _document;
        private ITextView _view;

        public AddMissingRulesAction(List<Keyword> missingRules, EditorConfigDocument document, ITextView view)
        {
            _missingRules = missingRules;
            _document = document;
            _view = view;
        }

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

            var addMissingRulesActionAll = new AddMissingRulesActionAll(_missingRules, _document, _view);

            List<Keyword> missingRulesDotNet = FindMissingRulesSpecific(Category.DotNet);
            List<Keyword> missingRulesCSharp = FindMissingRulesSpecific(Category.CSharp);
            List<Keyword> missingRulesVB = FindMissingRulesSpecific(Category.VisualBasic);

            AddMissingRulesActionDotNet addMissingRulesActionDotNet = null;
            AddMissingRulesActionCSharp addMissingRulesActionCSharp = null;
            AddMissingRulesActionVB addMissingRulesActionVB = null;
            if (missingRulesDotNet.Count() != 0)
            {
                addMissingRulesActionDotNet = new AddMissingRulesActionDotNet(missingRulesDotNet, _document, _view);
            }
            if (missingRulesCSharp.Count() != 0)
            {
                addMissingRulesActionCSharp = new AddMissingRulesActionCSharp(missingRulesCSharp, _document, _view);
            }
            if (missingRulesVB.Count() != 0)
            {
                addMissingRulesActionVB = new AddMissingRulesActionVB(missingRulesVB, _document, _view);
            }

            list.AddRange(CreateActionSet(addMissingRulesActionAll, addMissingRulesActionDotNet, addMissingRulesActionCSharp, addMissingRulesActionVB));
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(list);
        }
   
        public IEnumerable<SuggestedActionSet> CreateActionSet(params BaseSuggestedAction[] actions)
        {
            actions = actions.Where(val => val != null).ToArray();
            return new[] { new SuggestedActionSet(actions) };
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            // do nothing
        }

        internal static List<Keyword> FindMissingRulesAll(List<string> currentRules)
        {
            var missingRules = new List<Keyword>();
            var missingRuleNames = new List<string>();
            IEnumerator<Keyword> allRules = SchemaCatalog.VisibleKeywords.GetEnumerator();
            while (allRules.MoveNext())
            {
                string curRule = allRules.Current.Name.ToLower(CultureInfo.InvariantCulture);
                if (!currentRules.Contains(curRule) && !missingRuleNames.Contains(curRule) && !curRule.StartsWith("dotnet_naming") && !curRule.Equals("root") && !curRule.Equals("max_line_length"))
                {
                    missingRules.Add(allRules.Current);
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
