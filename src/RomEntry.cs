using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Quad64.src
{
    public class RomEntry : INotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public string RomName { get; set; }

        // 末尾の [XXXXXXXX] ハッシュと拡張子を除いたファイル名
        public string FileName
        {
            get
            {
                string name = Path.GetFileNameWithoutExtension(FilePath);
                // "[英数字]" を末尾から除去（例: " [A1B2C3D4]"）
                name = Regex.Replace(name, @"\s*\[[0-9A-Fa-f]+\]\s*$", "").TrimEnd();
                return name;
            }
        }

        public string DisplayName => FileName;

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
