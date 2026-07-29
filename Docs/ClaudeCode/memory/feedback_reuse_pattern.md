---
name: feedback_reuse_pattern
description: 이미 작동하는 코드 패턴이 있으면 동일한 방식으로 재사용할 것
type: feedback
originSessionId: 93ff8962-d236-4105-9ce1-30c9289c2e19
---
이미 작동하는 코드가 있으면 그 패턴을 그대로 복사해서 재사용할 것. 다른 방식으로 시도하지 말 것.
**Why:** SwapSlot에서 TileSlot과 다른 Canvas 탐색 방식을 사용해서 고스트가 안 보이는 문제로 오랜 시간을 낭비함.
**How to apply:** 같은 기능을 하는 코드를 새로 작성할 때, 프로젝트에 이미 작동하는 유사 코드가 있으면 그 패턴을 그대로 따를 것.
