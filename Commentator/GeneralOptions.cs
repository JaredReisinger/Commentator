using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Spudnoggin.Commentator
{
    class GeneralOptions : DialogPage
    {
        public GeneralOptions()
        {
            this.AutoWrapEnabled = true;
            this.AutoWrapColumn = 80;
            this.MinimumWrapWidth = 10;
            this.CodeWrapEnabled = false;
            this.AvoidWrappingBeforeLine = 1;
        }

        [Category("Automatic Wrapping")]
        [DisplayName("Enable automatic wrapping")]
        [Description("Whether automatic comment wrapping should occur.")]
        [DefaultValue(true)]
        public bool AutoWrapEnabled { get; set; }

        [Category("Automatic Wrapping")]
        [DisplayName("Wrapping column")]
        [Description("The column after which comments will wrap.")]
        [DefaultValue(80)]
        public int AutoWrapColumn { get; set; }

        [Category("Automatic Wrapping")]
        [DisplayName("Minimum wrapping width")]
        [Description("The minimum width for which wrapping will be enabled, for instance when the comment content is indented.")]
        [DefaultValue(10)]
        public int MinimumWrapWidth { get; set; }

        [Category("Automatic Wrapping")]
        [DisplayName("Wrap on lines with code")]
        [Description("Whether automatic wrapping should occur on lines that contain code."
            + " Note that this can result in unexpected behavior as aligned comments on"
            + " consecutive lines will be treated as a single, wrappable block.")]
        [DefaultValue(false)]
        public bool CodeWrapEnabled { get; set; }

        [Category("Automatic Wrapping")]
        [DisplayName("Avoid wrapping before line")]
        [Description("Avoids wrapping comments at the beginning of the file.  If you"
            + " use a file-header comment, you should set this to the first line number"
            + " on which you want wrapping to occur.")]
        [DefaultValue(1)]
        public int AvoidWrappingBeforeLine { get; set; }


        ////#region ICustomTypeDescriptor Members

        ////public AttributeCollection GetAttributes()
        ////{
        ////    return TypeDescriptor.GetAttributes(this, true);
        ////}

        ////public string GetClassName()
        ////{
        ////    return TypeDescriptor.GetClassName(this, true);
        ////}

        ////public string GetComponentName()
        ////{
        ////    return TypeDescriptor.GetComponentName(this, true);
        ////}

        ////public TypeConverter GetConverter()
        ////{
        ////    return TypeDescriptor.GetConverter(this, true);
        ////}

        ////public EventDescriptor GetDefaultEvent()
        ////{
        ////    return TypeDescriptor.GetDefaultEvent(this, true);
        ////}

        ////public PropertyDescriptor GetDefaultProperty()
        ////{
        ////    return TypeDescriptor.GetDefaultProperty(this, true);
        ////}

        ////public object GetEditor(Type editorBaseType)
        ////{
        ////    return TypeDescriptor.GetEditor(this, editorBaseType, true);
        ////}

        ////public EventDescriptorCollection GetEvents(Attribute[] attributes)
        ////{
        ////    return TypeDescriptor.GetEvents(this, attributes, true);
        ////}

        ////public EventDescriptorCollection GetEvents()
        ////{
        ////    return TypeDescriptor.GetEvents(this, true);
        ////}

        ////public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        ////{
        ////    var props = TypeDescriptor.GetProperties(this, attributes, true);
        ////    return props;
        ////}

        ////public PropertyDescriptorCollection GetProperties()
        ////{
        ////    var props = TypeDescriptor.GetProperties(this, true);
        ////    return props;
        ////}

        ////public object GetPropertyOwner(PropertyDescriptor pd)
        ////{
        ////    return this;
        ////}

        ////#endregion
    }
}
