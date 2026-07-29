---
name: Unity Editor.log 자동 참조
description: Unity 콘솔 로그가 필요한 상황에서 사용자 캡처 없이도 클로드가 자동으로 Editor.log 읽어 분석
type: reference
originSessionId: 93ff8962-d236-4105-9ce1-30c9289c2e19
---
Unity 에디터에서 발생하는 에러/경고/Debug.Log를 확인할 때 **사용자가 캡처 보내지 않아도 자동으로 Editor.log 파일 읽어서 분석한다.**

## 경로

`C:\Users\DG-2410-PC-053\AppData\Local\Unity\Editor\Editor.log`

Bash 경로: `/c/Users/DG-2410-PC-053/AppData/Local/Unity/Editor/Editor.log`

## 권장 명령

### PowerShell (권한 있음)
```powershell
# 최근 2000줄
Get-Content "C:\Users\DG-2410-PC-053\AppData\Local\Unity\Editor\Editor.log" -Tail 2000

# 특정 키워드 필터
Get-Content "C:\Users\DG-2410-PC-053\AppData\Local\Unity\Editor\Editor.log" -Tail 3000 | Select-String "BuildingView"
```

### Bash (대안)
```bash
tail -3000 "/c/Users/DG-2410-PC-053/AppData/Local/Unity/Editor/Editor.log" | grep -A 2 "BuildingView"
```

## 언제 자동 참조해야 하는가

- 사용자가 "에러 났다", "안 보인다", "Play 했더니", "콘솔에 ~~" 같은 표현 사용 시
- BuildingView, TopTowerIngame 등 우리가 추가한 Debug.Log 결과 확인 필요 시
- 사용자가 재테스트 결과 보고할 때 (캡처 없어도 우선 자동 확인)
- NullReferenceException, MissingReferenceException 등 Unity 런타임 에러 분석 시
- 컴파일 에러, asset import 에러 등 에디터 이슈

## 사용 팁

- 파일이 크면(16MB+) `tail` 또는 `Get-Content -Tail`로 **최근 부분만** 읽기
- Unity 에디터 실행 중에도 읽기 가능 (lock 안 됨)
- 키워드 필터링 후 결과가 많으면 `head -100` 등으로 제한
- 에러 스택 트레이스가 길면 `grep -A 5` 같이 컨텍스트 라인 포함

## How to apply

1. 사용자가 콘솔 관련 문제 제기 → 즉시 Editor.log 읽기 시도
2. 캡처 보내달라고 하지 말고 우선 자동 분석
3. 분석 결과 사용자에게 보고하면서 진행
4. 만약 Editor.log에 충분한 정보가 없으면 그때 사용자에게 추가 캡처 요청
