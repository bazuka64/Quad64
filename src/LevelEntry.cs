using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Quad64.src
{
    public class LevelEntry : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public ushort ID { get; set; }

        private BitmapSource _thumbnail;
        public BitmapSource Thumbnail
        {
            get => _thumbnail;
            set { _thumbnail = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
