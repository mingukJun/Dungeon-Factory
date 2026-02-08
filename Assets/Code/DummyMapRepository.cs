using UnityEngine;

public class DummyMapRepository : MonoBehaviour, IMapRepository
{
    public MapPopupData GetMapPopupData(string mapId)
    {
        var data = new MapPopupData
        {
            mapId = mapId,
            mapName = "고대 유적",
            subTitle = "NORMAL · 지역 1-3",
            recommended = new MapPopupData.RecommendedStats { atk = 120, def = 90, hp = 1500 },
            partyDeathProbability = 0.23f
        };

        data.drops.Add(new MapPopupData.DropEntry { itemId = "itm_iron", itemName = "철 조각", iconAddressKey = "icon/iron", probability = 0.42f });
        data.drops.Add(new MapPopupData.DropEntry { itemId = "itm_gem", itemName = "에메랄드", iconAddressKey = "icon/gem_emerald", probability = 0.07f });

        data.party.Add(new MapPopupData.PartySlot { memberId = "ply_001", displayName = "리나", portraitAddressKey = "portrait/lina", level = 12, role = "DPS" });
        data.party.Add(new MapPopupData.PartySlot { memberId = "ply_002", displayName = "카인", portraitAddressKey = "portrait/kain", level = 11, role = "TANK" });

        return data;
    }
}
