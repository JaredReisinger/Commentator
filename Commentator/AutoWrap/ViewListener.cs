using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Spudnoggin.Commentator.AutoWrap
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp"), ContentType("C/C++"), ContentType("Basic")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    class ViewListener : IWpfTextViewCreationListener
    {
        [Import]
        private SVsServiceProvider serviceProvider = null;

        [Import]
        private IClassifierAggregatorService aggregatorService = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Properties.GetOrCreateSingletonProperty(() =>
                new AutoWrapper(serviceProvider, aggregatorService, textView));
        }
    }
}
