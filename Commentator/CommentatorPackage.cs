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

namespace Spudnoggin.Commentator
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", AutoVersion.GeneratedConstants.InformationalVersion, IconResourceID = 400)]
    [ProvideOptionPage(typeof(GeneralOptions), AutoVersion.GeneratedConstants.Name, "General", 200, 201, false, 202)]
    [ProvideService(typeof(CommentatorService))]
    [Guid(GuidList.guidCommentatorPkgString)]
    public sealed class CommentatorPackage : Package
    {
        public CommentatorPackage()
        {
            ////Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        protected override void Initialize()
        {
            ////Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            var container = (IServiceContainer)this;
            container.AddService(typeof(CommentatorService), this.CreateService, true);
        }

        internal T GetDialogPage<T>()
            where T : DialogPage
        {
            return (T)this.GetDialogPage(typeof(T));
        }

        private object CreateService(IServiceContainer container, Type service)
        {
            if (typeof(CommentatorService) == service)
            {
                return new CommentatorService(this);
            }

            return null;
        }
    }
}
