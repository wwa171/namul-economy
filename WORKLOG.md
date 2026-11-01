# 📜 Namul RPG Core 개발 및 작업 내역 (WORKLOG)

이 문서는 추후 개발 재개 시 빠른 컨텍스트 파악 및 신규 기능 연계를 위해 지금까지 구현된 시스템, 아키텍처, 데이터베이스 스키마 및 향후 로드맵을 정리한 작업 내역서입니다.

---

## 📅 최근 주요 작업 완료 내역 요약 (2026-10-01)

### 1. 🏛️ 실시간 서버 경매장 시스템 (`/경매장`, `/경매`, `/ah`)
- **기능**: 유저 간 인게임 골드를 이용한 무기/장비 위탁 판매 및 실시간 구매.
- **아키텍처 및 구현**:
  - `Database/Models/AuctionListing.cs` 및 `Database/DatabaseManager.Auction.cs`: 경매 목록 SQLite CRUD 구현 (`AuctionListings` 테이블).
  - `Services/AuctionService.cs`: 등록/구매/취소 원자적 비즈니스 로직.
  - `Commands/CommandManager.cs`:
    - `/경매 목록 [페이지]`: 등록 물품 및 가격 페이지네이션 조회.
    - `/경매 등록 [가격]`: 손에 든 아이템의 수량/접두사(Prefix) 판별 후 인벤토리에서 안전 차감, DB 등록 및 SSC(서버 사이드 캐릭터) 강제 저장.
    - `/경매 구매 [물품번호]`: 구매자 골드 차감 ➡️ 판매자 골드 입금 ➡️ 구매자 인벤토리 빈 슬롯에 정확한 아이템 복원 지급 및 전체 브로드캐스트.

### 2. 💎 장비 드롭 무작위 스탯(Affix) 룰렛 시스템
- **기능**: 사냥 파밍의 재미를 극대화하기 위해 몬스터/보스 처치 시 장비 드롭에 무작위 접두사 부여.
- **구현**:
  - `Config/PluginConfig.cs`: `RandomAffixConfig` (활성화 여부, 부여 확률 `AffixChance: 0.4`).
  - `Handlers/GameHooksHandler.cs`: `OnItemDrop` 이벤트에서 무기/방어구/장신구 드롭 감지 시 40% 확률로 `Prefix(-1)` 재련 부여 및 골드 플로팅 텍스트 팝업 연출.

### 3. ⚔️ 레벨 단계별 직업 특화 심화 패시브 버프
- **기능**: 유저 성장(Lv.30, Lv.60)에 맞춘 직업별 고위 전투 패시브 상시 유지.
- **구현**:
  - `Handlers/GameHooksHandler.cs`의 `ApplyPassiveBuffs`:
    - **전사 (Warrior)**: 기본(철피부+재생) ➡️ Lv.30(Wrath, 공격력+10%) ➡️ Lv.60(Endurance, 받는 피해 -10%)
    - **궁수 (Ranger)**: 기본(신속+양궁) ➡️ Lv.30(Rage, 치명타율+10%) ➡️ Lv.60(Ammo Reservation, 탄약 20% 절약)
    - **마법사 (Sorcerer)**: 기본(마나재생+마법강화) ➡️ Lv.30(Clairvoyance, 마법 데미지/치명타) ➡️ Lv.60(Rage, 마법 치명타+10%)
    - **소환사 (Summoner)**: 기본(소환 강화+신속) ➡️ Lv.30(Wrath, 소환수 공격력+10%) ➡️ Lv.60(Endurance, 생존력 보강)

### 4. 🎣 낚시(Fishing) 생활 콘텐츠 다중 어뷰징 방지 (Anti-Exploit)
- **설계 원칙**: 판정 입력은 전부 **서버 관측값**만 사용한다. 클라이언트가 실어 보내는 드롭 좌표(`ItemDrop.Position`), 슬롯 선택, 손에 든 아이템은 모두 위조 가능하므로 근거로 쓰지 않는다.
- **1차 구현(폐기, `0c4ecf8` / `5cc62b6`)의 결함**:
  - `GetDataHandlers.ItemDrop`에 보상을 걸었으나, 이 패킷은 **인벤토리에서 아이템을 버릴 때** 발생한다. 낚시 성공은 인벤토리 슬롯 변화로만 나타나므로 훅이 틀렸다.
  - 좌표 검증 입력이 클라이언트 신고값이라 찌 좌표를 실어 보내면 통과했다.
  - 낚싯대 소지 검사는 그 순간의 슬롯만 보므로, 경매장에서 물고기를 사서 던지고 줍는 반복으로 우회 가능했다.
  - 활성 찌가 없으면 무조건 거부 → 찌가 먼저 소멸하는 정상 낚시에서 오탐.
  - `Main.maxProjectiles` 전수 스캔(패킷당 O(1000)).
- **2차 구현(현행)**:
  - **찌 생명주기 추적**: `NewProjectile`으로 찌 생성(소유자·좌표)을 기록하고, `ProjectileKill`로 회수 시점을 잡는다. 두 훅 모두 서버가 직접 받는 패킷이다.
  - **좌표·액체 검증**: 회수 시점의 찌 좌표는 서버의 투사체 사본에서 읽고, 그 타일의 액체 유무를 `Main.tile`로 확인한다. 물 밖 회수는 낚시가 아니므로 창을 열지 않는다.
  - **보상 시점**: 인벤토리에 물고기/상자가 등장하는 `PlayerSlot`에서, 열려 있는 창(`CatchWindowMs`, 기본 2000ms)이 있을 때만 지급한다.
  - **쿨타임**: `Environment.TickCount64`(단조 증가)로 계정별 최소 간격을 강제한다. NTP 역행으로 음수가 나오지 않는다.
  - **fail-safe**: 모든 훅에 try/catch. 설정 접근은 `PluginMain.Config?.Fishing`로 NRE를 차단하고, 예외 시 보상만 포기한다(킥·차단 없음).
  - **메모리 상한**: 찌 추적 512개, 쿨타임 5000계정에서 정리.
  - **설정**: `RequireBobberCatch`(기본 true), `CatchWindowMs`(기본 2000), `RequireFishingPoleHeld`(보조 검증), `MinFishingIntervalSeconds`(기본 2.5초).

### 5. 👑 보스 스케일링 & 🛡️ 장비 레벨 제한
- **보스 자연 스폰 차단 (`BlockNaturalBossSpawn`)**: 밤 시간 조건부 자연 출현 차단.
- **소환자 레벨 비례 보스 체력 증폭**: `보스 체력 + (체력 * (소환자 레벨 * 계수))`.
- **보스 소환 아이템 레벨 제한**: 아이템별 요구 레벨 미달 시 사용 취소 및 인벤토리 원복.
- **장비 착용/소지 레벨 제한 (`ItemRequirements`)**: 초반 장비(Lv.1~)부터 던전/하드모드/제니스(Lv.90)까지 슬롯 및 들고 있는 아이템 검사.

### 6. 🚀 CI/CD & 자동 배포 체계
- GitHub Actions 워크플로우(`.github/workflows/build.yml`)를 구성하여, `main` 브랜치 푸시 시:
  - .NET 9.0 기반 TShock 플러그인(`TShockEconomyExp.dll`) 빌드
  - WPF GUI 설정 툴(`NamulRpgConfigTool.exe`) 싱글 파일 빌드
  - GitHub Releases `latest` 태그로 자동 배포 완료

---

## 🔮 다음 작업 가능한 후보 목록 (TODO / Backlog)

1. **경매장 고도화**:
   - `/경매 취소 [번호]`: 판매자가 미판매 물품을 회수하는 기능.
   - 보관함/우편함 시스템: 오프라인 판매 대금 또는 취소 아이템 수령함.
2. **보스 레이드 전용 보상 테이블 커스텀**:
   - 보스 처치 시 기여도(딜량 순위) 비례 추가 보너스 상자 드롭.
3. **신규 직업군 추가**:
   - 던지는 무기(투척/도적) 또는 탱커/힐러 하이브리드 직업.
4. **추가 생활 콘텐츠**:
   - 요리(Buff Cooking), 농사/약초 재배 시 추가 RPG 스탯 보너스.

---

*최종 커밋: 낚시 판정 재작성 (서버 관측 기반 찌 생명주기 검증)*  
*배포 릴리스: https://github.com/wwa171/namul-economy/releases/tag/latest*
