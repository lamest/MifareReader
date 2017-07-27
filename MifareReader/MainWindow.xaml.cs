using System.Threading;
using System.Windows;
using MifareReaderLibriary;

namespace MifareReader
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IRFCardReader reader;

        public MainWindow()
        {
            reader = new MifareCardReader(MifareConfigurationFabric.GetDefaultConfig());
            InitializeComponent();
            var devices = reader.GetDevices();
            reader.StartScan(devices[1]);
            //var writer = new MifareCardWriter();
            //var writeConfig = MifareConfigurationFabric.GetWriteConfig();
            //writer.WriteDataAsync(devices[1], writeConfig.AuthKeys, "Looooong Test__Data111String222", new CancellationTokenSource().Token);
        }
    }
}