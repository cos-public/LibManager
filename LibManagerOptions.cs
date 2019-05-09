using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;
using System.Globalization;

namespace LibManager
{
    [Guid("EE625525-A699-4C64-96E5-72F0CD2F49AC")]
    public class LibManagerOptions : Microsoft.VisualStudio.Shell.UIElementDialogPage
    {
        private readonly LibManagerOptionsPage _page;
        private static string GeneralOptionsCollection = "LibManagerGeneral";

        public List<string> LibraryPackFiles { get; set; } = new List<string>();
        public bool VerboseOutput { get; set; } = false;

        public LibManagerOptions()
        {
            _page = new LibManagerOptionsPage();
        }

        protected override System.Windows.UIElement Child => _page;

        protected override void OnApply(PageApplyEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            LibraryPackFiles = _page.LibraryPackFiles;
            base.OnApply(e);
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            base.LoadSettingsFromStorage();

            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!userSettingsStore.PropertyExists(GeneralOptionsCollection, nameof(LibraryPackFiles)))
            {
                _page.LibraryPackFiles = new List<string>();
                return;
            }

            var converter = new StringArrayConverter();
            this.LibraryPackFiles = converter.ConvertFrom(
                userSettingsStore.GetString(GeneralOptionsCollection, nameof(LibraryPackFiles))) as List<string>;
            // for debug
            //LibraryPackFiles.Add("C:\\Users\\Username\\Projects\\lib_hl\\libraries.xml");

            _page.LibraryPackFiles = LibraryPackFiles;
        }

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            base.SaveSettingsToStorage();

            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!userSettingsStore.CollectionExists(GeneralOptionsCollection))
                userSettingsStore.CreateCollection(GeneralOptionsCollection);

            var converter = new StringArrayConverter();
            userSettingsStore.SetString(
                GeneralOptionsCollection,
                nameof(LibraryPackFiles),
                converter.ConvertTo(this.LibraryPackFiles, typeof(string)) as string);
        }

        class StringArrayConverter : TypeConverter
        {
            private const string delimiter = "#@#";

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(List<string>) || base.CanConvertTo(context, destinationType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                string v = value as string;

                if (v == null)
                    return new List<string>();
                else
                    return new List<string>(v.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries));
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                List<string> v = value as List<string>;
                if (destinationType != typeof(string) || v == null)
                {
                    return base.ConvertTo(context, culture, value, destinationType);
                }
                return string.Join(delimiter, v);
            }
        }
    }
}
