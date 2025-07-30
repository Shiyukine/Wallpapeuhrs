using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.XamlTypeInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wallpapeuhrs
{
    public class Program : Microsoft.UI.Xaml.Application, IXamlMetadataProvider
    {
        private static XamlControlsXamlMetaDataProvider? xamlMetaDataProvider = null;

        public Program(string[] args)
        {
            App.nargs = args;
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            XamlControlsXamlMetaDataProvider.Initialize();
            xamlMetaDataProvider = new();
            this.Resources.MergedDictionaries.Add(new Microsoft.UI.Xaml.Controls.XamlControlsResources());
        }

        [System.STAThreadAttribute()]
        public static void Main(string[] args)
        {
            //if (args.Length > 0 && args[0].Contains("wp") && Convert.ToInt32(args[6]) > 3) new WallpapeuhrsBG.App();
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var syncContext = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(syncContext);

                new Program(args);

                var currentApp = Microsoft.UI.Xaml.Application.Current;
                if (currentApp is not null)
                    currentApp.Exit();
            });
        }

        public IXamlType GetXamlType(Type type)
        {
            return xamlMetaDataProvider.GetXamlType(type);
        }

        public IXamlType GetXamlType(string fullName)
        {
            return xamlMetaDataProvider.GetXamlType(fullName);
        }

        public XmlnsDefinition[] GetXmlnsDefinitions()
        {
            return xamlMetaDataProvider.GetXmlnsDefinitions();
        }
    }
}
