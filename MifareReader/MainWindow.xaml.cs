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
            reader.StartScan();
        }
    }
}