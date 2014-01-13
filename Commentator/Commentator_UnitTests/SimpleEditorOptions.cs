using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;

namespace Commentator_UnitTests
{
    class SimpleEditorOptions : IEditorOptions
    {
        Dictionary<string, object> options = new Dictionary<string, object>();

        public bool ClearOptionValue<T>(EditorOptionKey<T> key)
        {
            return this.ClearOptionValue(key.Name);
        }

        public bool ClearOptionValue(string optionId)
        {
            return this.options.Remove(optionId);
        }

        public object GetOptionValue(string optionId)
        {
            if (this.options.ContainsKey(optionId))
            {
                return this.options[optionId];
            }
            else if (this.Parent != null)
            {
                return this.Parent.GetOptionValue(optionId);
            }

            throw new KeyNotFoundException(string.Format("could not find '{0}'", optionId));
        }

        public T GetOptionValue<T>(EditorOptionKey<T> key)
        {
            return (T)this.GetOptionValue(key.Name);
        }

        public T GetOptionValue<T>(string optionId)
        {
            return (T)this.GetOptionValue(optionId);
        }

        public IEditorOptions GlobalOptions
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsOptionDefined<T>(EditorOptionKey<T> key, bool localScopeOnly)
        {
            return this.IsOptionDefined(key.Name, localScopeOnly);
        }

        public bool IsOptionDefined(string optionId, bool localScopeOnly)
        {
            if (this.options.ContainsKey(optionId))
            {
                return true;
            }
            else if (!localScopeOnly && this.Parent != null)
            {
                return this.Parent.IsOptionDefined(optionId, localScopeOnly);
            }

            return false;
        }

        public event EventHandler<EditorOptionChangedEventArgs> OptionChanged;

        public IEditorOptions Parent { get; set; }

        public void SetOptionValue<T>(EditorOptionKey<T> key, T value)
        {
            this.SetOptionValue(key.Name, value);
        }

        public void SetOptionValue(string optionId, object value)
        {
            this.options[optionId] = value;
        }

        public IEnumerable<EditorOptionDefinition> SupportedOptions
        {
            get { throw new NotImplementedException(); }
        }
    }
}
