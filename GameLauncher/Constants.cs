using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLauncher
{
    class Constants
    {
        public static readonly string GAME_TITLE = "RobinMania";
        public static readonly string GAME_LAUNCHER = "RobinMania Launcher";

        //API 
        public static readonly string VERSION_DOWNLOAD_URI = "http://localhost:3000/download/version";
        public static readonly string GAME_DOWNLOAD_URI = "http://localhost:3000/download/game";
        public static readonly string NFTJSON_DOWNLOAD_URI = "http://localhost:3000/download/nftjson";
        public static readonly string NFTS_DOWNLOAD_URI = "http://localhost:3000/download/nfts";
        public static readonly string NFTS_DOWNLOAD_CUSTOM_URI = "http://localhost:3000/download/nfts/custom";

    }
}
