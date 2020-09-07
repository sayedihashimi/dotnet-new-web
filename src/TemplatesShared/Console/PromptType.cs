using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace TemplatesShared {
    public enum PromptType {
        TrueFalse   = 1,
        FreeText    = 2,
        PickOne     = 3,
        PickMany    = 4
    }

    public class Prompt<T> :Prompt{
        public T ParsedValue() {
            return (T)Result;
        }
    }

    public class TrueFalsePrompt : Prompt<bool> {
        public TrueFalsePrompt(string text) {
            Text = text;
            PromptType = PromptType.TrueFalse;
        }

        public bool DefaultValue { get; set; }
    }
    public class FreeTextPrompt : Prompt<string> {
        public FreeTextPrompt(string text, bool allowEmptyResponse = false) {
            Text = text;
            PromptType = PromptType.FreeText;
            AllowEmptyResponse = allowEmptyResponse;
        }
        public bool AllowEmptyResponse { get; set; }
    }
    public abstract class OptionsPrompt : Prompt<string> {
        public OptionsPrompt(string text, List<UserOption> userOptions) {
            Text = text;
            UserOptions = userOptions;
        }
        public List<UserOption> UserOptions { get; private set; }
    }
    public class PickOnePrompt : OptionsPrompt {
        public PickOnePrompt(string text,List<UserOption>userOptions) : base(text,userOptions) {
            PromptType = PromptType.PickOne;
        }
    }
    public class PickManyPrompt : OptionsPrompt {
        public PickManyPrompt(string text, List<UserOption> userOptions):base(text,userOptions) {
            PromptType = PromptType.PickMany;
        }
    }
    public class UserOption {
        public UserOption(string text, object value) {
            Text = text;
            Value = value;
        }
        public string Text { get; set; }
        public bool IsSelected { get; set; }
        // TODO: IsRequired Not used currently
        public bool IsRequired { get; set; }
        public object Value { get; set; }
        /// <summary>
        /// Will toggle the value of IsSelected and then return the new value
        /// </summary>
        /// <returns>New value for IsSelected</returns>
        public bool ToggleIsSelected() {
            IsSelected = !IsSelected;
            return IsSelected;
        }

        public override int GetHashCode() {
            return Text.GetHashCode() +
                    IsSelected.GetHashCode() +
                    IsRequired.GetHashCode() +
                    (Value != null ? Value.GetHashCode() : 0);
        }
        public override bool Equals(object obj) {
            UserOption other;
            if(obj == null || !(obj is UserOption)) {
                return false;
            }
            
            other = obj as UserOption;
            if (other == null) {
                return false;
            }

            return string.Equals(Text, other.Text) &&
                    IsSelected == other.IsSelected &&
                    IsRequired == other.IsRequired &&
                    (Value != null ? Value.Equals(other.Value) : other.Value == null);
        }

        public override string ToString() {
            return Text;
        }

        //public static List<UserOption> ConvertToOptions(List<string> optionsText) {
        //    var result = new List<UserOption>();
        //    foreach(var ot in optionsText) {
        //        result.Add(new UserOption(ot));
        //    }
        //    return result;
        //}
    }
    public class Prompt {
        public PromptType PromptType { get; protected set; }
        /// <summary>
        /// Text that will be shown to the user
        /// </summary>
        public string Text { get; protected set; }
        /// <summary>
        /// Result will be one of:
        ///  - boolean
        ///  - string
        ///  - list of string
        /// </summary>
        public object Result { get; set; }
    }


}
