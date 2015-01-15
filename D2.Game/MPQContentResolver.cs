using CrystalMpq;
using SharpDX.Toolkit.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2.Game
{
    public class MPQContentResolver : IContentResolver
    {
        public string RootDirectory { get; private set; }
        private MpqFileSystem fileSystem;

        public MPQContentResolver(string rootDirectory)
        {
            this.RootDirectory = rootDirectory;

            fileSystem = new MpqFileSystem();
            var archive = new MpqArchive(System.IO.Path.Combine(rootDirectory, "d2data.mpq"));
            fileSystem.Archives.Add(archive);
        }

        public bool Exists(string assetName)
        {
            foreach (var archive in fileSystem.Archives)
            {
                if (archive.FindFile(assetName.Substring("Content\\".Length)) != null)
                {
                    return true;
                }
            }

            return false;
        }

        public System.IO.Stream Resolve(string assetName)
        {
            foreach (var archive in fileSystem.Archives)
            {
                var file = archive.FindFile(assetName.Substring("Content\\".Length));
                if (file != null)
                {
                    return file.Open();
                }
            }

            return null;
        }
    }
}
