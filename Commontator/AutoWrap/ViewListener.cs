using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Spudnoggin.Commontator.AutoWrap
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("CSharp"), ContentType("C/C++"), ContentType("Basic")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    class ViewListener : IWpfTextViewCreationListener
    {
        [Import]
        IClassifierAggregatorService aggregator = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.Properties.GetOrCreateSingletonProperty(() => new AutoWrapper(textView, aggregator));
        }
    }
}
