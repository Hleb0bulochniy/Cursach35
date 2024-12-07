namespace MS_Back_Maps
{
    /*[System.Serializable]
    public class MapData
    {
        public string mapName { get; set; }
        public int bombCount { get; set; }
        public int mapID { get; set; }
        public MapType mapType { get; set; }
        public MapSize mapSize { get; set; }


        public int gamesSum { get; set; }
        public int wins { get; set; }
        public int loses { get; set; }
        public int openedTiles { get; set; }
        public int openedNumberTiles { get; set; }
        public int openedBlankTiles { get; set; } //поля тайлов
        public int flagsSum { get; set; }
        public int flagsOnBombs { get; set; }
        public int timeSpentSum { get; set; }
        public int averageTime { get; set; }
        public MapData() { }

        public MapData(string mapName, int bombCount, int mapID, MapSize mapSize, MapType mapType, int gamesSum, int wins, int loses, int openedTiles, int openedNumberTiles, int openedBlankTiles, int flagsSum, int flagsOnBombs, int timeSpentSum, int averageTime)
        {
            this.mapName = mapName;
            this.bombCount = bombCount;
            this.mapID = mapID;
            this.mapSize = mapSize;
            this.mapType = mapType;
            this.gamesSum = gamesSum;
            this.wins = wins;
            this.loses = loses;
            this.openedTiles = openedTiles;
            this.openedNumberTiles = openedNumberTiles;
            this.flagsSum = flagsSum;
            this.flagsOnBombs = flagsOnBombs;
            this.timeSpentSum = timeSpentSum;
            this.averageTime = averageTime;
        }
    }

    [System.Serializable]
    public class GameData
    {
        public string aToken { get; set; }
        public string rToken { get; set; }
        public int userID { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; } //нужно присылать только зашифрованный пароль А******
        public DateTime updateDate { get; set; }

        public MapSize lastGameSize { get; set; }
        public MapType lastGameType { get; set; }
        public string lastGameData { get; set; }
        public int lastGameTime { get; set; }

        public GameData() { }
        public List<MapData> mapDataList { get; set; } = new List<MapData>();

        public GameData(string aToken, string rToken, int userID, string username, string email, string password, MapSize lastGameSize, MapType lastGameType, string lastGameData)
        {
            this.aToken = aToken;
            this.aToken = rToken;
            this.userID = userID;
            this.username = username;
            this.email = email;
            this.password = password;

            this.lastGameSize = lastGameSize;
            this.lastGameType = lastGameType;
            this.lastGameData = lastGameData;
        }
    }
    [System.Serializable]
    public enum MapSize
    {
        Small, Middle, Big, None
    }

    [System.Serializable]
    public enum MapType
    {
        Classic, Cross, Bomb, None
    }*/

    [Serializable]
    public enum CustomMapType
    {
        Auto, Manual, Mixed, None
    }

    [Serializable]
    public class CustomMapData
    {
        public int mapID { get; set; }
        public string mapName { get; set; }
        public int bombCount { get; set; }
        public int mapSize { get; set; }
        public CustomMapType mapType { get; set; }


        public int creatorId { get; set; }
        public string creatorName { get; set; }
        public DateTime creationDate { get; set; }
        public int ratingSum { get; set; }
        public int ratingCount { get; set; }
        public int downloads {  get; set; }
        public string about { get; set; }

        public CustomMapData() { }

        public CustomMapData(int mapID, string mapName, int bombCount, int mapSize, CustomMapType mapType, int creatorId, DateTime creationDate, int ratingSum, int ratingCount, int downloads, string about)
        {
            this.mapID = mapID;
            this.mapName = mapName;
            this.bombCount = bombCount;
            this.mapSize = mapSize;
            this.mapType = mapType;
            this.creatorId = creatorId;
            this.creationDate = creationDate;
            this.ratingSum = ratingSum;
            this.ratingCount = ratingCount;
            this.downloads = downloads;
            this.about = about;
        }
    }
    [Serializable]
    public class IdModel
    {
        public int id { get; set; }
    }
    [Serializable]
    public class RateMap
    {
        public int mapId { get; set; }
        //public int oldRate { get; set; }
        public int newRate { get; set; }
    }
    [Serializable]
    public class MapSaveModel
    {
        public int id { get; set; }
        public int mapId { get; set; }
        public string mapName { get; set; }
        public int gamesSum { get; set; }
        public int wins {  get; set; }
        public int loses { get; set; }
        public int openedTiles { get; set; }
        public int openedNumberTiles { get; set; }
        public int openedBlankTiles { get; set; }
        public int flagsSum { get; set; }
        public int flagsOnBombs {  get; set; }
        public int timeSpentSum { get; set; }
        public int averageTime {  get; set; }
        public string lastGameData { get; set; }
        public int lastGameTime { get; set; }
    }
    public class MapSaveListModel
    {
        public List<MapSaveModel> mapSaveList { get; set; } = new List<MapSaveModel>();
    }


    //для кафки
    [Serializable]
    public class LogModel
    {
        public int userId { get; set; }
        public DateTime dateTime { get; set; }
        public string serviceName { get; set; }
        public string logLevel { get; set; }
        public string eventType { get; set; }
        public string message { get; set; }
        public string details { get; set; }
        public string errorCode { get; set; }
    }
}
