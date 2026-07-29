using System.Collections.Generic;
using UnityEngine;

namespace KS.TopTower
{
    // (참고) 엘리베이터 열 동적 계산은 StageData.GetElevatorColumn() 에 있음.

    // 큐브 크기 클래스. 그리드 칸 타입(CubeType: Background/Outdoor/Indoor)과는 별개 축.
    //  Long  = 1:1.3 세로 긴 큐브 (실내 모듈 몸통)
    //  Short = 1:0.43 세로 짧은 큐브 (구분줄/천장, fr류)
    //  Big   = Long/Short를 넘나드는 큰 크기 (Root, Cons 등)
    public enum CubeSize { Long, Short, Big }

    // 뼈대 — 실외/천장(구분줄 실내)/실내.
    public enum Frame { Outdoor, Bonedoor, Indoor }

    // 그룹 — 업종/분류.
    //  Structural : 외벽·옥상 등 아웃도어 위주 구조. 시스템 자동, 상호작용 없음.
    //  System     : 실내공사모듈·빈방·엘베 등 인도어 시스템 배치. 유저가 직접 못 짓지만 상호작용 가능(빈방→임차인, 엘베 업그레이드 등).
    //  Facility/Restaurant/Commercial/Office/Residence/Hotel : 임대 업종(임차인 찾기 대상).
    // (System을 enum 끝에 추가 — 기존 ModuleData의 직렬화 int 값 보존)
    public enum ModuleGroup { Structural, Facility, Restaurant, Commercial, Office, Residence, Hotel, System }

    // 확장 속성.
    public enum ModuleExtend { Normal, Terrace, Penthouse }

    /// <summary>
    /// 모듈 1개의 타입 속성(6축) 정의. 편집: Tools/Top Tower/Module Type Editor.
    /// Group/Module은 스프라이트 파일명에서 파싱된 값. Zone은 다중(allowedZones).
    /// 타입 정의는 스테이지 공통(모듈명 기준 1벌)으로 관리.
    /// </summary>
    [CreateAssetMenu(fileName = "MD_", menuName = "TopTower/Module Data", order = 1)]
    public class ModuleData : ScriptableObject
    {
        [Header("식별 (파일명에서 파싱)")]
        public ModuleGroup group;
        public string moduleName;

        [Header("타입 속성")]
        public CubeSize cube;
        public Frame frame;
        public List<Zone> allowedZones = new List<Zone>();
        public ModuleExtend extend = ModuleExtend.Normal;

        [Header("방 정보 (팝업 표시용 — 유저에게 보임)")]
        [Tooltip("클릭 시 방 정보 팝업 표시 여부. 엘베 등 시설은 기본 false.")]
        public bool clickable;
        [Tooltip("유저에게 보이는 방 이름 (파일/모듈명과 별개).")]
        public string roomName;
        [TextArea(2, 5)]
        [Tooltip("방 설명.")]
        public string description;
        [Tooltip("일일 임대료.")]
        public long dailyRent;

        [Header("건설 (임차인 입주)")]
        [Tooltip("공사 비용(재화 소모). 임대 업종 모듈용. 0이면 건설 대상 아님(구조/시스템 등).")]
        public long buildCost;
        [Tooltip("건설 소요 시간(초). 이 시간 뒤 실내공사모듈 → 실제 모듈로 전환.")]
        public float buildSeconds = 10f;

        [Header("참조 (미리보기)")]
        public Sprite sprite;
    }
}
