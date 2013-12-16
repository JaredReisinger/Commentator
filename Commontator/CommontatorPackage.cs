using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace Spudnoggin.Commontator
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "0.1.xxx", IconResourceID = 400)]
    [ProvideOptionPage(typeof(GeneralOptions), "Commontator", "General", 200, 201, false, 202)]
    [ProvideService(typeof(CommontatorService))]
    [Guid(GuidList.guidCommontatorPkgString)]
    public sealed class CommontatorPackage : Package
    {
        public CommontatorPackage()
        {
            ////Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        protected override void Initialize()
        {
            ////Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            var container = (IServiceContainer)this;
            container.AddService(typeof(CommontatorService), this.CreateService, true);
        }

        internal T GetDialogPage<T>()
            where T : DialogPage
        {
            return (T)this.GetDialogPage(typeof(T));
        }

        private object CreateService(IServiceContainer container, Type service)
        {
            if (typeof(CommontatorService) == service)
            {
                return new CommontatorService(this);
            }

            return null;
        }
    }
}
