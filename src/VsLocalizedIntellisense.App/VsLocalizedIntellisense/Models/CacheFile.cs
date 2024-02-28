using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VsLocalizedIntellisense.Models
{
    public interface ICachedTimestamp
    {
        #region property

        DateTimeOffset CachedTimestamp { get; set; }

        #endregion
    }

    public class CacheFile<T>
        where T : class, ICachedTimestamp
    {
        public CacheFile(string path, TimeSpan timeout)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path));
            }

            Path = path;
            Timeout = timeout;
        }

        #region property

        public string Path { get; }
        public TimeSpan Timeout { get; }

        #endregion

        #region function

        public T Read(DateTimeOffset timestamp)
        {
            if (!File.Exists(Path))
            {
                return null;
            }

            object raw = null;
            using (var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                raw = serializer.ReadObject(stream);
            }
            if (raw == null)
            {
                return null;
            }

            if (raw is T value)
            {
                var span = timestamp - value.CachedTimestamp;
                if (span < Timeout)
                {
                    return value;
                }
            }

            return null;
        }

        #endregion
    }
}
