using System.Text;

namespace PythonPackagePublishing
{
    class ZipEncoder : UTF8Encoding
    {
        public ZipEncoder()
        {

        }
        public override byte[] GetBytes(string s)
        {
            s = s.Replace("\\", "/");
            return base.GetBytes(s);
        }
    }
}
