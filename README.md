# TShockEconomyExp (경험치 & 경제 코어 플러그인)

TShock 최신 릴리즈(v6.2.1 / Terraria 1.4.5.8, .NET 9.0) 기반으로 제작된 고성능 모듈형 경험치 & 경제 코어 플러그인입니다.

---

## 🌟 주요 특징

1. **독립 SQLite 분리 저장 (`tshock/economy_exp/`)**
   - `economy_data.sqlite`: 유저 잔액, 최종 거래 일시
   - `exp_data.sqlite`: 유저 레벨, 누적 경험치, 최종 갱신 일시
   - 경제와 경험치 DB가 물리적으로 분리되어 있어 특정 데이터만 초기화(시즌 리셋)하거나 백업하기 매우 용이합니다.

2. **상점 및 외부 플러그인 연동 인터페이스 (`API/`)**
   - `IEconomyService`: 잔액 확인, 입금, 출금, 송금뿐만 아니라 **상점 전용 구매(`ProcessPurchase`) 및 판매(`ProcessSale`) 메서드**와 이벤트를 기본 제공합니다.
   - `IExpService`: 레벨, 누적 경험치, 다음 레벨 필요량, 랭킹 TOP10 조회 및 레벨업 이벤트를 제공합니다.
   - 다른 플러그인에서 `PluginMain.EconomyService` 및 `PluginMain.ExpService`를 바로 참조하여 사용 가능합니다.

3. **자동 몬스터 처치 보상 (`Handlers/GameHooksHandler.cs`)**
   - 일반 몬스터 처치 시 체력 비례 경험치 및 골드 자동 지급
   - 보스 몬스터 토벌 시 배율 적용 및 전체 브로드캐스트 공지
   - 신규 유저 접속 시 정착 지원금 자동 지급

---

## 🎮 인게임 명령어 안내

### 💰 경제 명령어
- `/돈` 또는 `/bal`, `/balance` [유저이름]: 본인 또는 대상 유저의 잔액 확인
- `/송금` 또는 `/pay` [받는사람] [금액]: 다른 유저에게 돈 송금
- `/돈지급` 또는 `/givemoney` [유저이름] [금액]: (관리자) 유저에게 돈 지급

### ⭐ 경험치 & 레벨 명령어
- `/레벨` 또는 `/경험치`, `/lvl`, `/level`, `/exp` [유저이름]: 현재 레벨, 현재 진행도/필요 경험치 확인
- `/랭킹` 또는 `/순위`, `/top`, `/rank`: 서버 레벨 랭킹 TOP 10 확인
- `/경험치지급` 또는 `/giveexp` [유저이름] [경험치]: (관리자) 유저에게 경험치 지급
- `/레벨설정` 또는 `/setlevel` [유저이름] [레벨]: (관리자) 유저 레벨 강제 지정

---

## ⚙️ 설정 파일 (`tshock/economy_exp/config.json`)

```json
{
  "CurrencyName": "골드",
  "StartingBalance": 1000,
  "BaseExpRequirement": 100,
  "ExpRequirementMultiplier": 1.25,
  "MaxLevel": 100,
  "EnableKillRewards": true,
  "NormalMonsterExpRatio": 0.1,
  "NormalMonsterMoneyRatio": 0.05,
  "BossRewardMultiplier": 5.0
}
```

---

## 🔨 빌드 방법

1. TShock 서버 폴더의 `TShockAPI.dll` 및 `TerrariaServer.dll`을 프로젝트 폴더 내 `lib/`에 배치합니다.
2. `dotnet build -c Release` 실행
3. `bin/Release/net9.0/TShockEconomyExp.dll`을 서버의 `ServerPlugins/` 폴더에 넣고 서버를 구동합니다.
