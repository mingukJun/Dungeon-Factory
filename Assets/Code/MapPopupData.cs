using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapPopupData
{
    public string mapId;
    public string mapName;
    public string subTitle;                  // 지역/난이도 등
    public RecommendedStats recommended;     // 적정 스탯
    public float partyDeathProbability;      // 0~1

    public List<DropEntry> drops = new();    // 드랍 목록
    public List<PartySlot> party = new();    // 파티 구성

    [Serializable] public class RecommendedStats { public int atk; public int def; public int hp; }
    [Serializable] public class DropEntry { public string itemId; public string itemName; public string iconAddressKey; public float probability; }
    [Serializable] public class PartySlot { public string memberId; public string displayName; public string portraitAddressKey; public int level; public string role; }
}
