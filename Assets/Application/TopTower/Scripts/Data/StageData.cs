using System.Collections.Generic;
using UnityEngine;

namespace KS.TopTower
{
    public enum Zone
    {
        Underground,
        Aboveground,
        Rooftop,
    }

    /// <summary>
    /// 엘리베이터 위치. Indoor 5칸(C, D, E, F, G) 중 C, E, G 세 위치만 선택 가능.
    /// - Left (C): 엘베가 Indoor 좌측 끝. 모듈은 1×4 (D-E-F-G).
    /// - Center (E): 엘베가 Indoor 가운데. 모듈은 1×2 두 개 (C-D, F-G).
    /// - Right (G): 엘베가 Indoor 우측 끝. 모듈은 1×4 (C-D-E-F).
    /// </summary>
    public enum ElevatorPosition
    {
        Left,
        Center,
        Right,
    }

    /// <summary>
    /// 한 Cube 칸의 타입. 한 층 안에서 자유 배치 가능.
    /// </summary>
    public enum CubeType
    {
        Background = 0,  // 배경 (하늘)
        Outdoor = 1,     // 외벽
        Indoor = 2,      // 실내 (모듈 배치 가능)
    }

    [System.Serializable]
    public class FloorData
    {
        public int FloorIndex;       // 지하층 음수, 지상층 양수, 0은 사용 안 함
        public Zone Zone;

        /// <summary>
        /// 한 층의 Cube 타입 배열. 화면 가로 = 9 Cube.
        /// 각 칸이 Background/Outdoor/Indoor 중 하나로 자유 배치됨.
        /// </summary>
        public CubeType[] Cubes = DefaultCubes();

        /// <summary>
        /// 기본 빌딩 레이아웃: [배경][외벽][Indoor×5][외벽][배경]
        /// </summary>
        public static CubeType[] DefaultCubes()
        {
            return new[]
            {
                CubeType.Background, // col 0
                CubeType.Outdoor,    // col 1
                CubeType.Indoor,     // col 2
                CubeType.Indoor,     // col 3
                CubeType.Indoor,     // col 4
                CubeType.Indoor,     // col 5
                CubeType.Indoor,     // col 6
                CubeType.Outdoor,    // col 7
                CubeType.Background, // col 8
            };
        }
    }

    [CreateAssetMenu(fileName = "StageData_", menuName = "TopTower/Stage Data", order = 0)]
    public class StageData : ScriptableObject
    {
        [Header("기본 정보")]
        public int StageID;
        public string StageName;     // 예: "시부야"

        [Header("엘리베이터 위치 (스테이지별 결정 가능, C/E/G 중 하나)")]
        public ElevatorPosition ElevatorPosition = ElevatorPosition.Left;

        [Header("모듈 슬롯 폭 (스테이지별)")]
        [Tooltip("이 스테이지에서 구조/엘베를 제외한 모든 모듈이 차지하는 가로 큐브 수. 예: 1스테이지=4. 엘베는 항상 1(예외).")]
        public int ModuleSlotWidth = 4;

        [Header("빌딩 레이아웃")]
        public List<FloorData> Floors = new List<FloorData>();
        // 위에서 아래 순서 또는 아래에서 위 순서 — 일관성 위해 FloorIndex로 정렬해서 사용

        [Header("옥상 클리어 옵션 (추후 ModuleData로 교체)")]
        public List<string> RooftopClearOptionsPlaceholder = new List<string>();
        // TODO: ModuleData ScriptableObject 만들면 List<ModuleData>로 변경

        /// <summary>
        /// 엘리베이터가 위치할 열(0-based). 고정 알파벳이 아니라 실제 Indoor 영역에서 계산.
        /// Left=인도어 최좌측, Right=인도어 최우측, Center=중앙 (전 층 통틀어 Indoor의 min/max 기준).
        /// 인도어 영역이 D~J든 C~K든 자동 대응. 인도어가 하나도 없으면 -1.
        /// </summary>
        public int GetElevatorColumn()
        {
            int min = int.MaxValue, max = int.MinValue;
            if (Floors != null)
            {
                foreach (var f in Floors)
                {
                    if (f == null || f.Cubes == null) continue;
                    for (int c = 0; c < f.Cubes.Length; c++)
                    {
                        if (f.Cubes[c] != CubeType.Indoor) continue;
                        if (c < min) min = c;
                        if (c > max) max = c;
                    }
                }
            }
            if (min > max) return -1;
            switch (ElevatorPosition)
            {
                case ElevatorPosition.Left:   return min;
                case ElevatorPosition.Right:  return max;
                case ElevatorPosition.Center: return (min + max) / 2;
                default: return min;
            }
        }
    }
}
