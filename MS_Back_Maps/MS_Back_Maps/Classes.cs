namespace MS_Back_Maps
{
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
    [Serializable]
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

    [Serializable]
    public class UserIdCheckModel
    {
        public string requestId { get; set; }
        public int userId { get; set; }
        public bool isValid { get; set; }
        public string userName { get; set; }
    }
}
