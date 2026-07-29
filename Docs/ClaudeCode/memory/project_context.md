---
name: 프로젝트 배경
description: 새 게임 "Top Tower" 개발 - 회사 빈 껍데기(newbon_group3) 베이스, 패키지명 com.kyeongsoo.toptower
type: project
originSessionId: 93ff8962-d236-4105-9ce1-30c9289c2e19
---
이 워킹 디렉토리(newbon_group3)는 사용자가 다니던/다니는 회사의 "빈 껍데기" 프로젝트다. 기본적인 게임 구조와 시스템은 남아있지만, 사용자는 이를 베이스로 완전히 새로운 게임 **"Top Tower"** 를 만든다.

- **새 게임 이름**: Top Tower
- **패키지명(applicationIdentifier)**: com.kyeongsoo.toptower
- **회사 원본 이름**: Newbon_Group3 (제거 대상)
- **회사 원본 패키지**: com.doubleugames.ngfe.newbon_group3 계열 (제거 대상)

**Why:** 기존 코드/에셋이 "회사 원본 그대로"가 아니라 "재활용 가능한 토대"로 다뤄져야 한다. 남아있는 구조가 새 게임에 맞지 않으면 과감히 들어내거나 개조해도 된다.

**How to apply:**
- 기존 시스템을 보존해야 하는 레거시로 가정하지 말 것. 사용자가 새 게임 방향에 따라 재설계/삭제할 수 있다.
- 코드를 읽을 때 "이게 원래 회사 게임의 일부였구나"라는 점을 감안해서 설명하기.
- 새 기능을 제안할 때, 기존 구조와 충돌하면 어떤 부분을 바꾸면 되는지도 같이 짚어주기.
- 게임의 새로운 방향(장르/규칙/타깃)이 정해지면 별도 메모리로 추가 저장.
- 빌드 산출물 이름이 newbon_group3로 나오는 원인은 `Assets/Settings/BuildServiceSettings/01.Dev|02.Qa|03.Live.asset`의 productName/applicationIdentifier 필드. 회사 패키지 com.doubleugames.buildservice의 BuildManual.cs:206이 빌드 시 PlayerSettings를 이 값으로 강제 덮어쓴다.
