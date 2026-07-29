# Top Tower — 모듈 타입 체계 / 좌표계 (재설계 정본)

> 본 문서 범위: 빌딩 시각 시스템의 **좌표계**와, 큐브를 점유하는 모든 요소(모듈)에 붙는 **6개 타입(축)** 의 정본.
> 기존 `Glossary.md`/`Groups.md`/`Zones.md`/`Modules.md`와 충돌 시 **본 문서를 우선**한다.
>
> 구현 상태: 상당 부분이 아직 설계 단계다. 현재 코드는 9칸 그리드 + 스프라이트 이름 매칭(`_Type_`) 기반이며, 아래 신규 개념(13칸, Frame/Cube/Extend를 데이터로 다루기, ModuleData 등)은 단계적으로 이식 예정.

---

## 1. 좌표계

- **열 (Column)** — 가로 위치. **알파벳 A~M, 총 13개.** 왼→오.
- **행 (Row)** — 세로 위치. **행은 "층(Floor)" 단위로 센다.**
- **크기 표기** — `N × M` = **세로 N행(층) × 가로 M열.**

### 1.1 행의 내부 구성 (롱/숏)

세로 줄은 두 종류가 교대로 쌓인다. 한 "층"은 이 둘의 세트다.

- **롱행 (Long)** — 큐브 몸통. 비율 **1 : 1.3**(세로가 김). 실내 모듈·외벽(Wall류)이 점유.
- **숏행 (Short)** — 구분줄(천장). 비율 **1 : 0.43**(세로가 짧음). 천장(Floor)·fr류가 점유.
- **층 (Floor) = 롱행 1 + 그 위 숏행 1.** 숏행(천장)은 **자기 아래 층의 소유**다.

```
   숏행 ▶ [········· 천장(구분줄) ·········]   ┐
   롱행 ▶ [········· 큐브 몸통 ···········]   ┘ = 1층
   숏행 ▶ [········· 천장 ···············]   ┐
   롱행 ▶ [········· 큐브 몸통 ···········]   ┘ = 1층
```

### 1.2 그리드 13칸 배치

```
   A  B  C │ D │ E  F  G  H  I │ J │ K  L  M
   └배경3─┘ │외벽│  └─실내 5──┘ │외벽│ └배경3─┘
```

- **A B C / K L M** (양옆 3칸씩) = 배경(빈칸). *과거 1칸 → 3칸으로 확대. 이유: 추후 야외 에셋 시스템, 화면 축소 시 빌딩 주변 배경 조망.*
- **D / J** = 외벽 시작 (좌/우).
- **E F G H I** = 실내 5칸. *단 빌딩 설계에 따라 E~I에도 외벽이 올 수 있음.*
- **엘리베이터 컬럼**: 실내 5칸 중 **E(Left) / G(Center) / I(Right)**.

---

## 2. 모듈에 붙는 6개 타입(축)

큐브를 점유하는 모든 요소는 다음 6개 타입을 가진다.

| # | 축 | 값 |
|---|-----|-----|
| 1 | **Cube 큐브** | Long / Short / Big |
| 2 | **Zone 존** | Aboveground / Underground / Rooftop |
| 3 | **Frame 뼈대** | Outdoor / Bonedoor / Indoor |
| 4 | **Group 그룹** | Structural / Facility / Restaurant / Commercial / Office / Residence / Hotel |
| 5 | **Module 모듈** | 개별 객체명 (거의 파일명과 동일) |
| 6 | **Extend 확장** | Terrace / Penthouse / Normal |

### 2.1 Cube 큐브 (크기 클래스)

- **Long** — 1:1.3, 세로가 긴 큐브. 실내 모듈이 들어감.
- **Short** — 1:0.43, 세로가 짧은 큐브. 천장 묘사. 거의 모든 상황에서 fr류가 점유.
- **Big** — Long/Short를 넘나드는 큰 크기. 대표: 뿌리코어(Root), 옥상건설(Cons/Consroof).

### 2.2 Zone 존 (지상/지하/옥상)

- **Aboveground 지상존** — 일반 모듈 자유 배치.
- **Underground 지하존** — 일반 모듈 자유 배치.
- **Rooftop 옥상존** — 평상시 ConstructionModule(공사중)이 자동 점유. 증축 한계치 도달 시 공사중이 사라지고 옥상이 덮임. 덮인 이후 펜트하우스 건설 가능.

**지상/지하 경계 규칙 (정본):**
> **1층 롱행(몸통)의 바로 밑부터 = 지하 판정.** 즉 1F 몸통 아래에 오는 숏행(B1 천장 또는, 지하가 없으면 Bottom)부터 그 아래 전부가 지하다. (1F의 천장 숏행은 지상.)

### 2.3 Frame 뼈대 (실외/천장/실내)

Zone이 지상/지하를 나눈다면, Frame은 실내/실외/천장을 나눈다.

- **Outdoor** — 실외(외벽·옥상). 일반적인 방법으로 실내 모듈을 지을 수 없음. (스프라이트 목록은 §3)
- **Bonedoor** — 천장부분(=구분줄). 실내 천장. 스프라이트는 `Floor`.
- **Indoor** — 실내. 각종 가게·실내 모듈이 들어감.

> 주의: 구분줄(숏행) 중 **실내 부분만 Bonedoor**이고, 그 줄의 외벽 부분(`Wallfr`/`Underwallfr`/`Bottomfr`)은 **Frame = Outdoor**다.

### 2.4 Group 그룹 (업종/분류)

- **Structural 구조** — 시스템이 자동 배치하는 구조 요소. 유저가 짓거나 임대 안 함. **Frame=Outdoor 요소 전부 + 천장(Floor) 등**이 여기 속함.
- **Facility 시설** — 건물 운영용. **엘리베이터·빈방(Empty) 모듈이 여기 속함.** *(과거 Structural이었으나 Facility로 변경.)*
- **Restaurant 식당** — 예: 초밥집
- **Commercial 상업** — 예: 서점
- **Office 사무** — 예: 보험사
- **Residence 주거** — 예: 투룸 *(과거 Residential에서 스펠링 변경.)*
- **Hotel 호텔** — *내용 미정(추가 예정).*

### 2.5 Module 모듈 (개별 객체명)

- 그룹 안의 개별 객체 이름. 파일 이미지명이 되기도 한다.
- **명명 규칙: `nnn_Group_Name_nnn`**
  - 예: `nnn_Structural_Wall_nnn`, `nnn_Facility_Elevator_nnn`, `nnn_Restaurant_Sushi_nnn`, `nnn_Commercial_Book_nnn`, `nnn_Office_Bank_nnn`, `nnn_Residence_1level_nnn`, `nnn_Hotel_1level_nnn`

### 2.6 Extend 확장

- **Terrace 테라스** — 실내 모듈이 **인접 좌/우 외벽(Outdoor) 칸 1개**를 추가 점유.
- **Penthouse 펜트하우스** — **옥상 Outdoor 칸** 추가 점유. **항상 옥상존 전용**, Group은 자유(주거/사무 펜트하우스 등).
- **Normal 노말** — 테라스/펜트하우스가 아닌 모든 것.

---

## 3. Frame=Outdoor 스프라이트 목록 (Structural 그룹)

| 스프라이트명 | 한글 | 큐브 | 위치/조건 |
|---|---|---|---|
| `nnn_Structural_Wall_nnn` | 외벽 | Long | 지상 외벽(몸통) |
| `nnn_Structural_Gate_nnn` | 현관 | Long | 1F 외벽 자리 (Wall보다 우선) |
| `nnn_Structural_Underwall_nnn` | 지하외벽 | Long | 지하 외벽(몸통) |
| `nnn_Structural_Wallfr_nnn` | 천장외벽 | Short | 지상 외벽의 구분줄 |
| `nnn_Structural_Underwallfr_nnn` | 지하천장외벽 | Short | 지하 외벽의 구분줄 |
| `nnn_Structural_Bottom_nnn` | 바닥 | Short | 최하 구분줄 실내 |
| `nnn_Structural_Bottomfr_nnn` | 바닥외벽 | Short | 최하 구분줄 외벽 *(신규 — 현재 코드는 임시로 Underwallfr 재사용)* |
| `nnn_Structural_Cons_nnn` | 공사 | Big | 항상 최상층 위 공사구간. **2 × 7** 소모 |
| `nnn_Structural_Consroof_nnn` | 옥상공사 | Big | 증축 한계치 최상층 공사구간. **1 × 7** 소모 |
| `nnn_Structural_Root_nnn` | 뿌리 | Big | 항상 빌딩 최하단(지하). **2 × 7** |

- **좌측 스프라이트만 등록하면 우측은 런타임 미러**로 자동 생성 (Wall/Wallfr/Underwall/Underwallfr/Gate/Bottomfr 등).
- 천장 실내(Bonedoor)는 `nnn_Structural_Floor_nnn`, Short 큐브.

---

## 4. 타입 매핑 예시

| 모듈 | Cube | Zone | Frame | Group | Module | Extend |
|------|------|------|-------|-------|--------|--------|
| `Facility_Empty` | Long | 지상·지하 | Indoor | Facility | Empty | Normal |
| `Facility_Elevator` | Long | 지상·지하 | Indoor | Facility | Elevator | Normal |
| `Structural_Root` | Big | 지하 | Outdoor | Structural | Root | Normal |
| `Structural_Bottomfr` | Short | 지하 | Outdoor | Structural | Bottomfr | Normal |

> 엘베·빈방은 옥상존 미포함(지상·지하만). Root/Bottomfr는 "1F 몸통 밑" 규칙에 의해 지하 판정.
