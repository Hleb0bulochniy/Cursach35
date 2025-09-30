using MS_Back_Maps.Models;
using System.Text.Json.Serialization;

namespace MS_Back_Maps
{
    [Serializable]
    public enum CustomMapType
    {
        Auto, Manual, Mixed, None
    }

    [Serializable]
    public class CustomMapDataDTO
    {
        public int MapID { get; set; }
        public string MapName { get; set; }
        public int BombCount { get; set; }
        public int MapSize { get; set; }
        public CustomMapType MapType { get; set; }


        public int CreatorId { get; set; }
        public string CreatorName { get; set; }
        public DateTime CreationDate { get; set; }
        public int RatingSum { get; set; }
        public int RatingCount { get; set; }
        public int Downloads {  get; set; }
        public string About { get; set; }

        public CustomMapDataDTO() { }

        public CustomMapDataDTO(int mapID, string mapName, int bombCount, int mapSize, CustomMapType mapType, int creatorId, DateTime creationDate, int ratingSum, int ratingCount, int downloads, string about)
        {
            this.MapID = mapID;
            this.MapName = mapName;
            this.BombCount = bombCount;
            this.MapSize = mapSize;
            this.MapType = mapType;
            this.CreatorId = creatorId;
            this.CreationDate = creationDate;
            this.RatingSum = ratingSum;
            this.RatingCount = ratingCount;
            this.Downloads = downloads;
            this.About = about;
        }
    }
    [Serializable]
    public class RateMapDTO
    {
        private int _newRate;
        public int MapId { get; set; }
        public int NewRate
        {
            get
            {
                return _newRate;
            }
            set
            {
                if (value < 1) value = 1;
                if (value > 5) value = 5;
                _newRate = value;
            }
        }

        public RateMapDTO() { }
        public RateMapDTO(int mapID, int newRate)
        {
            this.MapId = mapID;
            this.NewRate = newRate;
        }
    }
    [Serializable]
    public class MapSaveModelDTO
    {
        public int Id { get; set; }
        public int MapId { get; set; }
        public string MapName { get; set; }
        public int GamesSum { get; set; }
        public int Wins {  get; set; }
        public int Loses { get; set; }
        public int OpenedTiles { get; set; }
        public int OpenedNumberTiles { get; set; }
        public int OpenedBlankTiles { get; set; }
        public int FlagsSum { get; set; }
        public int FlagsOnBombs {  get; set; }
        public int TimeSpentSum { get; set; }
        public string LastGameData { get; set; }
        public int LastGameTime { get; set; }

        public MapSaveModelDTO() { }
        public MapSaveModelDTO(int id, int mapId, string mapName, int gamesSum, int wins, int loses, int openedTiles, int openedNumberTiles, int openedBlankTiles, int flagsSum, int flagsOnBombs, int timeSpentSum, string lastGameData, int lastGameTime)
        {
            this.Id = id;
            this.MapId = mapId;
            this.MapName = mapName;
            this.GamesSum = gamesSum;
            this.Wins = wins;
            this.Loses = loses;
            this.OpenedTiles = openedTiles;
            this.OpenedNumberTiles = openedNumberTiles;
            this.OpenedBlankTiles = openedBlankTiles;
            this.FlagsSum = flagsSum;
            this.FlagsOnBombs = flagsOnBombs;
            this.TimeSpentSum = timeSpentSum;
            this.LastGameData = lastGameData;
            this.LastGameTime = lastGameTime;
        }
    }
    [Serializable]
    public class MapSaveListModelDTO
    {
        public List<MapSaveModelDTO> MapSaveList { get; set; } = new List<MapSaveModelDTO>();
        public MapSaveListModelDTO() { }
        public MapSaveListModelDTO(List<MapSaveModelDTO> mapSaveList)
        {
            this.MapSaveList = mapSaveList;
        }
    }


    //для кафки
    [Serializable]
    public class LogModel
    {
        public int UserId { get; set; }
        public DateTime DateTime { get; set; }
        public string ServiceName { get; set; }
        public string LogLevel { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string ErrorCode { get; set; }
    }

    [Serializable]
    public class UserIdCheckModel
    {
        public string requestId { get; set; }
        public string requestMessage { get; set; }
        public int? userId { get; set; }
        public int? playerId { get; set; }
        public int? creatorId { get; set; }
        public bool isValid { get; set; }
        public string userName { get; set; }
    }


    //DTO
    [Serializable]
    public class ResponseDTO //для обычных ответов на Ok(), BadRequest() и т.д.
    {
        public string Message { get; set; } //сюда будет писаться сообщение из лога logModel.message
        public ResponseDTO() { }
        public ResponseDTO(string message)
        {
            Message = message;
        }
    }
}
