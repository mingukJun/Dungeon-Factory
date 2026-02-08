public interface IMapRepository
{
    // BGDatabase에서 mapId로 전부 채워 반환
    MapPopupData GetMapPopupData(string mapId);
}
