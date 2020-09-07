using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TemplatesShared {
    public class SharPromptInvoker : IPromptInvoker {
        public Prompt GetPromptResult(Prompt prompt) {
            Debug.Assert(prompt != null);

            switch (prompt.PromptType) {
                case PromptType.FreeText:
                    GetFreeTextPromptResult(prompt as FreeTextPrompt);
                    break;
                case PromptType.TrueFalse:
                    GetTrueFalsePromptResult(prompt as TrueFalsePrompt);
                    break;
                case PromptType.PickOne:
                    GetPickOnePromptResult(prompt as PickOnePrompt);
                    break;
                case PromptType.PickMany:
                    GetPickManyPromptResult(prompt as PickManyPrompt);
                    break;
            }

            return prompt;
        }

        protected Prompt GetFreeTextPromptResult(FreeTextPrompt prompt) {
            Debug.Assert(prompt != null);

            var result = Sharprompt.Prompt.Input<string>(prompt.Text);

            prompt.Result = result;
            return prompt;
        }
        protected Prompt GetTrueFalsePromptResult(TrueFalsePrompt prompt) {
            Debug.Assert(prompt != null);
            var result = Sharprompt.Prompt.Confirm(prompt.Text, prompt.DefaultValue);
            prompt.Result = result;
            return prompt;
        }

        protected Prompt GetPickOnePromptResult(PickOnePrompt prompt) {
            Debug.Assert(prompt != null);
            Debug.Assert(prompt.UserOptions != null && prompt.UserOptions.Count > 0);

            // reset the results before we do anything
            prompt.UserOptions.ForEach((uo) => uo.IsSelected = false);

            var result = Sharprompt.Prompt.Select(prompt.Text, prompt.UserOptions);
            if(result != null) {    
                result.IsSelected = true;
            }
            else {
                throw new NotImplementedException();
            }

            return prompt;
        }

        protected Prompt GetPickManyPromptResult(PickManyPrompt prompt) {
            Debug.Assert(prompt != null);
            Debug.Assert(prompt.UserOptions != null && prompt.UserOptions.Count > 0);

            // reset the results before we do anything
            prompt.UserOptions.ForEach((uo) => uo.IsSelected = false);

            var result = Sharprompt.Prompt.MultiSelect(prompt.Text, prompt.UserOptions).ToList();
            if(result != null) {
                result.ForEach((uo) => uo.IsSelected = true);
            }

            return prompt;
        }

        public List<Prompt> GetPromptResult(List<Prompt> prompts) {
            Debug.Assert(prompts != null);

            foreach(var prompt in prompts) {
                GetPromptResult(prompt);
            }

            return prompts;
        }
    }
}
