using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Spudnoggin.Commontator
{
    [Guid(GuidList.CommontatorServiceString)]
    internal class CommontatorService
    {
        private CommontatorPackage package;

        public CommontatorService(CommontatorPackage package)
        {
            this.package = package;
        }

        public GeneralOptions GetOptions()
        {
            return this.package.GetDialogPage<GeneralOptions>();
        }
    }
}
