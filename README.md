# PokeRoGue

포켓몬식 1:1 턴제 온라인 대전 게임이다. Unity 클라이언트와 C# 게임 서버로 만든 1인 프로젝트다.
명중·급소·데미지·상성·상태이상은 서버가 계산하고, 클라이언트는 결과 패킷을 받아 연출만 한다.

## 담당과 기반

- **담당**: 대전 패킷 프로토콜 설계, 서버 턴 판정·상태이상 처리, 클라이언트 대전 연출
- **기반**: Rookiss의 인프런 MMORPG 강의 시리즈의 서버·클라이언트 골격 — `ServerCore`(소켓·세션), 잡 큐, `AccountServer`·`SharedDB`, `PacketGenerator`, 클라이언트 매니저와 UI 프레임워크. 강의 원본 이력은 `master` 브랜치에 있다.

## 구성

| 경로 | 내용 |
| --- | --- |
| `Client/` | Unity 클라이언트 |
| `Server/Server/` | 게임 서버. TCP 7777. 로비, 대전 신청, 턴 판정 |
| `Server/AccountServer/` | 계정 생성·로그인 웹 API (ASP.NET Core) |
| `Server/ServerCore/` | 소켓·세션 라이브러리 |
| `Server/SharedDB/` | 두 서버가 함께 쓰는 DB 모델(로그인 토큰, 게임 서버 목록) |
| `Server/PacketGenerator/` | `Protocol.proto`에서 패킷 매니저 코드를 생성한다 |
| `Server/DummyClient/` | 콘솔 테스트 클라이언트 |
| `Common/protoc-3.12.3-win64/bin/` | `Protocol.proto`와 생성 스크립트 `GenProto.bat` |

## 요구 사항

- Windows
- .NET SDK 8 이상. 대상 프레임워크는 `net8.0`이다.
- SQL Server Express LocalDB(`(localdb)\MSSQLLocalDB`). Visual Studio 설치 관리자의 "데이터 스토리지 및 처리" 워크로드나 SQL Server Express 설치 관리자로 설치한다.
- Unity 6000.0.29f1

## 실행

명령은 저장소 루트에서 실행한다.

### 1. 도구와 인증서

```
dotnet tool restore
dotnet dev-certs https --trust
```

`AccountServer`는 `https://localhost:5001`로 요청을 받는다. 클라이언트가 접속하려면 개발 인증서를 신뢰해야 한다.

### 2. 데이터베이스

```
dotnet ef database update --project Server/Server
dotnet ef database update --project Server/AccountServer --context AppDbContext
dotnet ef database update --project Server/SharedDB --startup-project Server/AccountServer --context SharedDbContext
```

`GameDB`, `AccountDB`, `SharedDB`가 생긴다.

### 3. 서버

터미널 두 개에서 하나씩 실행한다.

```
dotnet run --project Server/AccountServer
dotnet run --project Server/Server
```

게임 서버는 10초마다 자기 주소를 `SharedDB`에 등록하고, 클라이언트는 로그인할 때 그 목록의 첫 서버로 접속한다. 게임 서버를 띄우고 10초가 지난 뒤에 로그인한다.

### 4. 클라이언트

대전에는 클라이언트가 두 개 필요하다. Unity에서 `Client` 폴더를 열고 Windows 빌드를 하나 만들어(`File > Build Profiles`) 에디터와 함께 띄운다. 시작 씬은 `Assets/Scenes/Main.unity`다.

양쪽에서 계정을 만들고(Create) 로그인하면 로비로 들어간다. 한쪽이 상대를 골라 대전을 신청하고 다른 쪽이 수락하면, 포켓몬 선택을 거쳐 대전이 시작된다.

## 패킷 수정

1. `Common/protoc-3.12.3-win64/bin/Protocol.proto`를 고친다.
2. `Server/PacketGenerator`를 빌드한다. 실행 파일이 `Server/PacketGenerator/bin/`에 생긴다.
3. `Protocol.proto`가 있는 폴더에서 `GenProto.bat`을 실행한다. `Protocol.cs`와 패킷 매니저가 생성되어 클라이언트, 게임 서버, `DummyClient`로 복사된다.

## 현재 상태

리팩토링 중이다. 알려진 문제:

- 게임 서버가 시작 직후 `Listener` 생성자 예외로 종료된다.
- 게임 서버는 `Dns.GetHostEntry(...).AddressList[1]` 주소에서 수신하고 그 주소를 `SharedDB`에 등록한다. 네트워크 구성에 따라 클라이언트가 접속하지 못할 수 있다.
- `Server/Server/Packet/ServerPacketManager.cs`는 생성 결과를 손으로 고친 상태라, `GenProto.bat`을 다시 실행하면 그 변경이 되돌아간다.
