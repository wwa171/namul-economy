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
- **발견된 취약점 방어**:
  - **인벤토리 드롭 악용 방지**: 손에 낚싯대(`_fishingPoleItemIds`)를 쥐고 있을 때만 드롭 보상 인정.
  - **초고속 매크로/핵 연타 방지**: 계정별 최소 낚시 쿨타임(`MinFishingIntervalSeconds: 2.5초`) 적용.
  - **타인 아이템 양도 어뷰징 완벽 차단**: 낚싯대만 들고 있을 때 다른 유저가 다가와 물고기/상자를 던져주는 꼼수를 막기 위해, 플레이어 본인이 물속에 띄운 활성 찌(`Bobber`, 11종 찌 투사체)가 존재하며 드롭된 좌표(`args.Position`)가 찌 위치 반경 10타일(160px) 이내에서 발생한 경우에만 보상 지급하도록 물리 좌표 정밀 일치 검증 도입.

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

*최종 커밋: `5cc62b6` (낚시 어뷰징 정밀 방어)*  
*배포 릴리스: https://github.com/wwa171/namul-economy/releases/tag/latest*
