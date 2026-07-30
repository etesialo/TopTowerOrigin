using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KS.TopTower.EditorTools
{
    public class StageBuilderTool : EditorWindow
    {
        // 데이터 그리드 가로 = 좌배경1 + 좌외벽1 + Indoor5 + 우외벽1 + 우배경1 = 9
        private const int GridWidth = 9;
        // 미리보기 표시용: 양옆에 빈 배경 여백 2칸씩 추가 → 총 13칸(A~M). 데이터(9칸)엔 영향 없음(표시 전용).
        private const int PadEach = 2;
        private const int DisplayWidth = GridWidth + PadEach * 2; // 13

        // Cube 시각 픽셀 크기 (1:1.3 비율)
        private const float CubeWidth = 40f;
        private const float CubeHeight = 52f;

        // 각 Cube는 Background/Outdoor/Indoor 중 하나로 자유 배치
        // 좌클릭으로 타입 순환: Background → Outdoor → Indoor → Background

        private StageData _stageData;
        private int _stageNumberInput = 1;
        private Vector2 _scrollPos;

        [MenuItem("Tools/Top Tower/Stage Builder")]
        public static void OpenWindow()
        {
            var window = GetWindow<StageBuilderTool>("Stage Builder");
            window.minSize = new Vector2(500, 700);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Top Tower - Stage Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 에셋 드래그 (드래그 시 아래 번호 자동 동기)
            EditorGUI.BeginChangeCheck();
            _stageData = (StageData)EditorGUILayout.ObjectField("Stage Data", _stageData, typeof(StageData), false);
            if (EditorGUI.EndChangeCheck() && _stageData != null)
                _stageNumberInput = ParseStageNumber(_stageData.name, _stageNumberInput);

            // 숫자 입력으로 해당 스테이지 에셋 자동 로드
            EditorGUI.BeginChangeCheck();
            _stageNumberInput = EditorGUILayout.IntField("스테이지 번호로 불러오기", _stageNumberInput);
            if (EditorGUI.EndChangeCheck())
                LoadStageByNumber(_stageNumberInput);

            if (_stageData == null)
            {
                EditorGUILayout.HelpBox("Stage Data를 선택하거나 새로 만드세요.", MessageType.Info);
                if (GUILayout.Button("New Stage Data 생성"))
                {
                    CreateNewStageData();
                }
                return;
            }

            EditorGUILayout.Space();
            DrawStageInfo();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "사용법:\n" +
                "  • 좌클릭 → 외벽↔실내 토글 (배경에서 시작 시 외벽)\n" +
                "  • 우클릭 → 무조건 배경으로 변경\n" +
                "  • 배경=하늘색, 외벽=검정, 실내=흰색\n" +
                "  • Zone은 자동 결정 (최상층=RT, 양수=AG, 음수=UG). 변경 불가.\n" +
                "  • 버튼으로 층 추가/삭제",
                MessageType.Info);
            EditorGUILayout.Space();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawGrid();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            DrawButtons();
        }

        private void DrawStageInfo()
        {
            EditorGUI.BeginChangeCheck();
            int newID = EditorGUILayout.IntField("Stage ID", _stageData.StageID);
            string newName = EditorGUILayout.TextField("Stage Name", _stageData.StageName);
            if (EditorGUI.EndChangeCheck())
            {
                _stageData.StageID = newID;
                _stageData.StageName = newName;
                EditorUtility.SetDirty(_stageData);
            }

            // 엘리베이터 위치 선택 (Indoor 5칸 C/D/E/F/G 중 C, E, G만 허용)
            EditorGUI.BeginChangeCheck();
            int currentIdx = (int)_stageData.ElevatorPosition; // 0=Left(C), 1=Center(E), 2=Right(G)
            string[] labels = { "C (좌측)", "E (가운데) — 모듈은 1×2 두 개", "G (우측)" };
            int newIdx = EditorGUILayout.Popup("엘리베이터 위치", currentIdx, labels);
            if (EditorGUI.EndChangeCheck())
            {
                _stageData.ElevatorPosition = (ElevatorPosition)newIdx;
                EditorUtility.SetDirty(_stageData);
            }
        }

        /// <summary>
        /// 엘리베이터가 차지하는 그리드 컬럼 인덱스 (9칸 그리드 기준).
        /// Indoor 5칸은 col 2~6 (C=2, D=3, E=4, F=5, G=6).
        /// </summary>
        private int GetElevatorColumn()
        {
            // Indoor 영역에서 동적 계산 (고정 알파벳 X). StageData가 담당.
            return _stageData.GetElevatorColumn();
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField("빌딩 그리드 (가로 A~M, 양옆 ABC·KLM=빈 배경 여백, 세로 층 번호)", EditorStyles.boldLabel);

            if (_stageData.Floors == null || _stageData.Floors.Count == 0)
            {
                EditorGUILayout.HelpBox("층이 없습니다. '층 추가' 버튼으로 추가하세요.", MessageType.Info);
                return;
            }

            // 그리기 전 Zone 자동 갱신 (최상층=RT, 양수=AG, 음수=UG)
            AutoUpdateZones();

            // 상단 알파벳 헤더 (A~I)
            DrawColumnHeader();

            // 위에서 아래로 그리기 (높은 FloorIndex 먼저)
            var sortedFloors = _stageData.Floors.OrderByDescending(f => f.FloorIndex).ToList();

            foreach (var floor in sortedFloors)
            {
                DrawFloorRow(floor);
            }
        }

        /// <summary>
        /// Zone 자동 갱신: 최상층=Rooftop, 양수 FloorIndex=Aboveground, 음수=Underground.
        /// </summary>
        private void AutoUpdateZones()
        {
            if (_stageData.Floors == null || _stageData.Floors.Count == 0) return;

            int maxIndex = _stageData.Floors.Max(f => f.FloorIndex);
            bool changed = false;

            foreach (var floor in _stageData.Floors)
            {
                Zone newZone;
                if (floor.FloorIndex == maxIndex)
                    newZone = Zone.Rooftop;
                else if (floor.FloorIndex < 0)
                    newZone = Zone.Underground;
                else
                    newZone = Zone.Aboveground;

                if (floor.Zone != newZone)
                {
                    floor.Zone = newZone;
                    changed = true;
                }
            }

            if (changed) EditorUtility.SetDirty(_stageData);
        }

        private void DrawColumnHeader()
        {
            EditorGUILayout.BeginHorizontal();
            // 좌측 라벨 자리 — 행 라벨과 동일한 폭. GetRect로 예약해 GUIStyle margin 누적 오차 방지.
            GUILayoutUtility.GetRect(70, 18, GUILayout.Width(70), GUILayout.Height(18));

            // 셀(DrawCube/DrawPaddingCell)과 동일하게 GetRect로 컬럼 폭을 예약해야
            // 실제 그리드와 픽셀 단위로 정확히 정렬된다. GUILayout.Label은 스타일 margin이
            // 컬럼마다 누적되어 오른쪽으로 갈수록 그리드와 어긋난다.
            var centerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            for (int dc = 0; dc < DisplayWidth; dc++)
            {
                Rect rect = GUILayoutUtility.GetRect(CubeWidth, 18, GUILayout.Width(CubeWidth), GUILayout.Height(18));
                string letter = ((char)('A' + dc)).ToString();
                GUI.Label(rect, letter, centerStyle);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFloorRow(FloorData floor)
        {
            EditorGUILayout.BeginHorizontal();

            // 좌측 행 라벨: 층 번호 + Zone 약자 (Zone은 자동 계산, 클릭 불가)
            string label = $"{floor.FloorIndex}\n{ZoneShort(floor.Zone)}";
            var labelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            GUILayout.Label(label, labelStyle, GUILayout.Width(70), GUILayout.Height(CubeHeight));

            for (int dc = 0; dc < DisplayWidth; dc++)
            {
                // 양옆 여백(A,B / L,M)은 빈 배경(비편집), 가운데 9칸이 실제 데이터(C~K).
                if (dc < PadEach || dc >= PadEach + GridWidth)
                    DrawPaddingCell(floor);
                else
                    DrawCube(dc - PadEach, floor);
            }

            EditorGUILayout.EndHorizontal();
        }

        private string ZoneShort(Zone zone)
        {
            switch (zone)
            {
                case Zone.Underground: return "UG";
                case Zone.Aboveground: return "AG";
                case Zone.Rooftop: return "RT";
                default: return "?";
            }
        }

        /// <summary>편집 불가 빈 배경 여백 칸 (A,B / L,M). 지하는 갈색, 지상은 하늘색.</summary>
        private void DrawPaddingCell(FloorData floor)
        {
            Rect rect = GUILayoutUtility.GetRect(CubeWidth, CubeHeight, GUILayout.Width(CubeWidth), GUILayout.Height(CubeHeight));
            EditorGUI.DrawRect(rect, BackgroundColor(floor));
            DrawRectBorder(rect, new Color(0f, 0f, 0f, 0.4f));
        }

        /// <summary>배경칸 색: 지하(FloorIndex&lt;0)는 갈색, 그 외 하늘색.</summary>
        private Color BackgroundColor(FloorData floor)
        {
            return floor.FloorIndex < 0
                ? new Color(0.60f, 0.45f, 0.32f)   // 갈색 (지하 지면)
                : new Color(0.50f, 0.85f, 1f);      // 하늘색
        }

        private void DrawCube(int col, FloorData floor)
        {
            EnsureCubesValid(floor);

            Rect rect = GUILayoutUtility.GetRect(CubeWidth, CubeHeight, GUILayout.Width(CubeWidth), GUILayout.Height(CubeHeight));

            // 명확한 색으로 셀 채우기 (배경칸은 지하=갈색 / 지상=하늘색)
            CubeType cellType = floor.Cubes[col];
            Color cellColor = cellType == CubeType.Background ? BackgroundColor(floor) : GetCubeColor(cellType);
            EditorGUI.DrawRect(rect, cellColor);

            // 엘리베이터 칸 표시 (Indoor 칸이면서 엘베 위치 컬럼)
            bool isElevatorCol = (col == GetElevatorColumn()) && floor.Cubes[col] == CubeType.Indoor;
            if (isElevatorCol)
            {
                // 노란색 반투명 오버레이
                EditorGUI.DrawRect(rect, new Color(1f, 0.85f, 0.2f, 0.45f));
                // "ELEV" 라벨
                var elevStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.black },
                };
                GUI.Label(rect, "ELEV", elevStyle);
            }

            // 외곽선 (검은 1px)
            DrawRectBorder(rect, new Color(0f, 0f, 0f, 0.4f));

            // 클릭 인식: 좌클릭 = 외벽↔실내 토글, 우클릭 = 배경으로
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.button == 0)
                {
                    HandleLeftClick(floor, col);
                    EditorUtility.SetDirty(_stageData);
                    Event.current.Use();
                    Repaint();
                }
                else if (Event.current.button == 1)
                {
                    HandleRightClick(floor, col);
                    EditorUtility.SetDirty(_stageData);
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        /// <summary>
        /// 좌클릭: 외벽↔실내 토글. Background 상태에서는 Outdoor부터 시작.
        /// </summary>
        private void HandleLeftClick(FloorData floor, int col)
        {
            var current = floor.Cubes[col];
            switch (current)
            {
                case CubeType.Background:
                    floor.Cubes[col] = CubeType.Outdoor;
                    break;
                case CubeType.Outdoor:
                    floor.Cubes[col] = CubeType.Indoor;
                    break;
                case CubeType.Indoor:
                    floor.Cubes[col] = CubeType.Outdoor;
                    break;
            }
        }

        /// <summary>
        /// 우클릭: 무조건 Background로 변경.
        /// </summary>
        private void HandleRightClick(FloorData floor, int col)
        {
            floor.Cubes[col] = CubeType.Background;
        }


        private void EnsureCubesValid(FloorData floor)
        {
            if (floor.Cubes == null || floor.Cubes.Length != GridWidth)
            {
                floor.Cubes = FloorData.DefaultCubes();
                EditorUtility.SetDirty(_stageData);
            }
        }

        private CubeType CycleCubeType(CubeType current)
        {
            int enumCount = System.Enum.GetValues(typeof(CubeType)).Length;
            return (CubeType)(((int)current + 1) % enumCount);
        }

        private void DrawRectBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }

        private Color GetCubeColor(CubeType type)
        {
            switch (type)
            {
                case CubeType.Background: return new Color(0.5f, 0.85f, 1f);    // 하늘색
                case CubeType.Outdoor:    return new Color(0.15f, 0.15f, 0.15f); // 검정 (외벽)
                case CubeType.Indoor:     return Color.white;                    // 흰색 (실내)
                default: return Color.magenta;
            }
        }

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("층 추가"))
            {
                AddFloorOnTop();
            }
            if (GUILayout.Button("지하층 추가"))
            {
                AddFloorOnBottom();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("최상층 삭제"))
            {
                RemoveTopFloor();
            }
            if (GUILayout.Button("최하층 삭제"))
            {
                RemoveBottomFloor();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("저장 (Apply)", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(_stageData);
                AssetDatabase.SaveAssetIfDirty(_stageData);
            }
        }

        private void AddFloorOnTop()
        {
            if (_stageData.Floors == null) _stageData.Floors = new System.Collections.Generic.List<FloorData>();

            int newIndex;
            Zone newZone = Zone.Aboveground;

            if (_stageData.Floors.Count == 0)
            {
                newIndex = 1; // 첫 층은 1층
            }
            else
            {
                int maxIndex = _stageData.Floors.Max(f => f.FloorIndex);
                newIndex = maxIndex + 1;
                if (newIndex == 0) newIndex = 1; // 0층은 건너뜀 (지하 -1 → 1층 직진)
                // 새 층 Zone은 현재 최상층 Zone 따라감
                var topFloor = _stageData.Floors.OrderByDescending(f => f.FloorIndex).First();
                newZone = topFloor.Zone;
            }

            _stageData.Floors.Add(new FloorData
            {
                FloorIndex = newIndex,
                Zone = newZone,
                Cubes = FloorData.DefaultCubes(),
            });
            EditorUtility.SetDirty(_stageData);
        }

        private void RemoveTopFloor()
        {
            if (_stageData.Floors == null || _stageData.Floors.Count == 0) return;

            var topFloor = _stageData.Floors.OrderByDescending(f => f.FloorIndex).First();
            _stageData.Floors.Remove(topFloor);
            EditorUtility.SetDirty(_stageData);
        }

        private void AddFloorOnBottom()
        {
            if (_stageData.Floors == null) _stageData.Floors = new System.Collections.Generic.List<FloorData>();

            int newIndex;
            Zone newZone = Zone.Underground;

            if (_stageData.Floors.Count == 0)
            {
                newIndex = -1; // 첫 지하층
            }
            else
            {
                int minIndex = _stageData.Floors.Min(f => f.FloorIndex);
                newIndex = minIndex - 1;
                if (newIndex == 0) newIndex = -1; // 0층 건너뜀
                // 새 지하층 Zone은 현재 최하층 Zone 따라감
                var bottomFloor = _stageData.Floors.OrderBy(f => f.FloorIndex).First();
                newZone = bottomFloor.Zone;
            }

            _stageData.Floors.Add(new FloorData
            {
                FloorIndex = newIndex,
                Zone = newZone,
                Cubes = FloorData.DefaultCubes(),
            });
            EditorUtility.SetDirty(_stageData);
        }

        private void RemoveBottomFloor()
        {
            if (_stageData.Floors == null || _stageData.Floors.Count == 0) return;

            var bottomFloor = _stageData.Floors.OrderBy(f => f.FloorIndex).First();
            _stageData.Floors.Remove(bottomFloor);
            EditorUtility.SetDirty(_stageData);
        }

        private const string StageDataFolder = "Assets/Application/TopTower/StageData";

        /// <summary>숫자 → StageData/Stage_{NNN}.asset 자동 로드.</summary>
        private void LoadStageByNumber(int number)
        {
            if (number == 0) return;
            string path = $"{StageDataFolder}/Stage_{number:D3}.asset";
            var sd = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (sd != null) { _stageData = sd; EditorGUIUtility.PingObject(sd); }
            else Debug.LogWarning($"[StageBuilderTool] 스테이지 에셋 없음: {path}");
        }

        /// <summary>"Stage_001" 등 이름 끝 숫자 추출. 실패 시 fallback.</summary>
        private static int ParseStageNumber(string name, int fallback)
        {
            int us = name.LastIndexOf('_');
            if (us < 0 || us == name.Length - 1) return fallback;
            string tail = name.Substring(us + 1);
            int end = 0;
            while (end < tail.Length && char.IsDigit(tail[end])) end++;
            if (end == 0) return fallback;
            return int.TryParse(tail.Substring(0, end), out int n) ? n : fallback;
        }

        private void CreateNewStageData()
        {
            EnsureFolderExists(StageDataFolder);

            int nextNumber = FindNextStageNumber();
            string fileName = $"Stage_{nextNumber:D3}";
            string newPath = $"{StageDataFolder}/{fileName}.asset";

            var newStageData = ScriptableObject.CreateInstance<StageData>();
            newStageData.StageID = nextNumber;
            newStageData.StageName = fileName;

            AssetDatabase.CreateAsset(newStageData, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _stageData = newStageData;
            EditorGUIUtility.PingObject(newStageData);
            Debug.Log($"[StageBuilderTool] Created new Stage Data: {newPath}");
        }

        private void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            // 부모 폴더부터 차례로 생성
            string[] parts = folderPath.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private int FindNextStageNumber()
        {
            int maxNumber = 0;
            string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { StageDataFolder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path);
                if (filename.StartsWith("Stage_"))
                {
                    string numberPart = filename.Substring("Stage_".Length);
                    if (int.TryParse(numberPart, out int num) && num > maxNumber)
                    {
                        maxNumber = num;
                    }
                }
            }
            return maxNumber + 1;
        }
    }
}
