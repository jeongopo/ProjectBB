# ProjectBB

스타듀밸리, 데이브 더 다이버 같은 2D 타이쿤(경영) 게임. 사냥/채집으로 재료를 얻고, 요리해서 손님에게 판매하는 루프가 중심이다.

- 엔진: Unity 6 (`6000.3.8f1`), 2D 기능 패키지, Input System 1.18, uGUI, TextMesh Pro
- 외부 플러그인: DOTween (`Assets/Plugins/Demigiant`)
- 언어: C#
- 타겟 화면 해상도: 800x450 (16:9). UI(Canvas Scaler 기준 해상도), 카메라, 스프라이트 크기는 이 해상도 기준으로 잡는다.
- 픽셀아트: 타일 1칸 = 16px, Pixels Per Unit = 16. Pixel Perfect Camera의 Reference Resolution은 800x450, Filter Mode는 Point.
  - 임포트 설정은 `Assets/Editor/PixelArtImportSettings.cs`가 `Assets/Art/PP_Interiors`와 `Assets/Resources/Art/UI` 아래 텍스처에 자동 적용한다 (Sprite, PPU 16, Point). `Assets/Resources/Art/Map`은 슬라이스를 유지한 채 PPU 16/Point만 적용한다.
  - 씬의 Grid Cell Size는 1x1로 통일한다. 이미 있는 텍스처는 메뉴 `Tools/Pixel Art/1. Reapply Import Settings`로 재적용한다.

## 디렉터리 구조

| 경로 | 내용 |
|---|---|
| `Assets/Scripts/Cooking` | 요리 미니게임 (썰기, 끓이기, 플레이팅, 타이머) |
| `Assets/Scripts/Hunting` | 사냥/채집 |
| `Assets/Scripts/Interaction` | 상호작용 (`IInteractable`, `InteractComponent`) |
| `Assets/Scripts/Inventory` | 인벤토리 |
| `Assets/Scripts/GamePlay` | 게임 상태, 입력, 플레이어, 씬, 타임라인 |
| `Assets/Scripts/Data` | XML 데이터 로딩, 세이브/로드, enum 정의 |
| `Assets/Scripts/UI` | 공용 UI 요소 |
| `Assets/Editor` | 픽셀아트 임포트/슬라이스/타일 에디터 도구 |
| `Assets/Resources/XML` | 런타임에 로딩되는 게임 데이터 (자동 생성물) |
| `Data/Excel` | 게임 데이터 원본 (xlsx) |
| `Tools/ExcelDataTool`, `ExcelToXmlTool` | xlsx → XML 변환 도구 |

## 데이터 파이프라인

- 기획 데이터의 원본은 `Data/Excel/*.xlsx`이다. `Assets/Resources/XML/*.xml`은 변환 결과물이므로 **XML을 직접 수정하지 말고** xlsx를 고친 뒤 변환 도구로 다시 생성한다.
- 런타임 로딩은 `DataManager.LoadDefineData<T>()`가 `Resources/XML/`에서 읽는다.
- 데이터 enum은 `DataEnumDefines` 네임스페이스(`DataEnumDefines.xlsx`에서 생성)에 있다. 수동 편집 시 xlsx와 어긋나지 않게 주의한다.
- 사용자에게 보이는 텍스트는 코드에 하드코딩하지 않고 `BBString` 데이터로 관리한다.

## 코드 스타일

Unity 기본 C# 규약(Microsoft C# 규약 기반)을 따른다.

### 네이밍
- 클래스, 구조체, enum, 메서드, 프로퍼티, 이벤트: `PascalCase`
- 인터페이스: `I` 접두사 + `PascalCase` (`IInteractable`)
- private/protected 필드: `_camelCase`
- 지역 변수, 매개변수: `camelCase`
- 상수: `PascalCase` (`const int MaxSlotCount = 20;`)
- 파일명은 클래스명과 일치시킨다. 파일 하나에 클래스 하나가 기본이다 (작은 보조 타입 제외).

### 포맷
- 들여쓰기는 스페이스 4칸, 중괄호는 Allman 스타일(여는 중괄호를 새 줄에).
- `if`/`for`/`foreach`는 한 줄이어도 중괄호를 쓴다.
- 접근 제한자는 항상 명시한다.
- `using`은 파일 맨 위, 사용하지 않는 `using`은 제거한다.
- 새 파일은 위 규칙을 따른다. 기존 파일에 탭/스페이스가 섞여 있어도 건드리는 범위를 넘어 전체를 재포맷하지 않는다.

### Unity 관례
- 인스펙터 노출은 `public` 필드 대신 `[SerializeField] private _field`를 쓴다. 외부 읽기가 필요하면 프로퍼티로 노출한다.
- 관련 필드는 `[Header]`로 묶는다.
- `Update`에서 `GetComponent`, `Find*`, LINQ, 문자열 연결 등 매 프레임 할당/탐색을 하지 않는다. `Awake`/`Start`에서 캐싱한다.
- 태그 비교는 `CompareTag`, 코루틴/타이머/트윈은 오브젝트 파괴 시 정리한다.
- 새 입력은 Input System 기준으로 작성한다 (`UnityEngine.Input` 구 API 사용 금지).
- 기본 애니메이션(UI 전환, 이동, 크기/색 변화, 피드백 연출 등)은 DOTween으로 작성한다. 단순한 연출에 Animator나 코루틴을 새로 만들지 않는다.
  - `using DG.Tweening;`을 사용하고, 트윈을 필드에 보관해야 하면 `Tween`/`Sequence` 타입으로 둔다.
  - 여러 단계는 `Sequence`로 묶고, 지속시간/Ease 값은 `[SerializeField]`로 노출해 인스펙터에서 조정할 수 있게 한다.
  - 같은 대상에 트윈이 겹치지 않도록 새 트윈 시작 전에 `Kill()`하고, `OnDestroy`/`OnDisable`에서 `Kill()`한다. 대상에 `SetLink(gameObject)`를 거는 방법도 가능하다.
  - 일시정지 중에도 재생해야 하는 UI 연출은 `SetUpdate(true)`를 사용한다.
  - 트윈 콜백(`OnComplete` 등)에서 파괴된 오브젝트를 참조하지 않도록 주의한다.
- 컴포넌트 단위로 책임을 나눈다 (`CuttingComponent`, `BoilingComponent`처럼). 거대한 Manager에 로직을 몰아넣지 않는다.
- 네임스페이스는 기존 구조(`GamePlay`, `Interaction`, `DataEnumDefines`, `GameEnumDefines`)를 따른다.

## 주석 규칙

**무의미한 주석은 쓰지 않고, 보이면 제거한다.**

- 코드를 그대로 설명하는 주석(`// 체력 감소`, `// Example usage ...`), 주석 처리된 죽은 코드, 템플릿에서 남은 주석은 삭제한다.
- 이름을 잘 지어 주석이 필요 없게 만드는 것을 우선한다.
- 주석은 "왜"가 코드만으로 드러나지 않을 때만 쓴다 (우회책, 비직관적인 제약, 밸런스 의도 등).
- 공개 API에는 필요한 경우에만 `/// <summary>` XML 문서 주석을 쓴다. 자명한 멤버에는 달지 않는다.
- 언어는 한국어로 쓴다.
- 작업 중 수정하는 파일에서 무의미한 주석을 발견하면 해당 범위 안에서 같이 정리한다. 관계없는 파일을 주석 정리만을 위해 건드리지 않는다.

## 작업 시 주의

- 요청받은 범위만 수정한다. 요청하지 않은 리팩터링, 대규모 재포맷은 하지 않는다.
- `.meta` 파일은 Unity가 관리한다. 에셋/스크립트를 추가하거나 삭제하면 대응하는 `.meta`도 함께 다룬다 (스크립트의 GUID가 바뀌면 프리팹/씬 참조가 끊어진다).
- `.unity`, `.prefab`은 YAML이지만 손으로 편집하지 않는다. 씬/프리팹 변경이 필요하면 무엇을 바꿔야 하는지 안내한다.
- `Library`, `Temp`, `Logs`, `UserSettings`는 건드리지 않는다.
- 커밋 메시지는 한국어, 브랜치/이슈는 `#번호` 형식을 참고한다 (`#40 ...`).
- 코드를 수정한 뒤 Unity에서 컴파일 확인은 사용자가 한다. 컴파일 에러 가능성이 있는 변경은 미리 알린다.
