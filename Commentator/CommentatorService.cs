using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Spudnoggin.Commentator
{
    [Guid(GuidList.CommentatorServiceString)]
    internal class CommentatorService
    {
        private CommentatorPackage package;

        public CommentatorService(CommentatorPackage package)
        {
            this.package = package;
        }

        public GeneralOptions GetOptions()
        {
            return this.package.GetDialogPage<GeneralOptions>();
        }
    }
}
