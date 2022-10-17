using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GI_AssetHasher
{
    public class AssetIndex
    {
        public Dictionary<string, string> Types { get; set; }
        public Dictionary<int, List<SubAssetInfo>> SubAssets { get; set; }
        public Dictionary<int, List<int>> Dependencies { get; set; }
        public List<uint> PreloadBlocks { get; set; }
        public List<uint> PreloadShaderBlocks { get; set; }
        public Dictionary<int, BlockInfo> Assets { get; set; }
        public List<uint> SortList { get; set; }
        public class SubAssetInfo
        {
            public string Name { get; set; }
            public byte PathHashPre { get; set; }
            public uint PathHashLast { get; set; }
        }
        public class BlockInfo
        {
            public byte Language { get; set; }
            public uint Id { get; set; }
            public uint Offset { get; set; }
        }
    }
}
