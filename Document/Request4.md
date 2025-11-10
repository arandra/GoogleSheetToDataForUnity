# GoogleSheetToDataForUnity 준비 요청서 (Request #4)

## 추진 방향
- 새 Unity 전용 저장소 `https://github.com/arandra/GoogleSheetToDataForUnity.git`에서 패키지 이름 `com.arandra.gsheet-to-data`로 구성
- 기존 Core 저장소 `https://github.com/arandra/GoogleSheetToData.git`를 서브모듈(`core/`)로 포함
- Google API DLL은 Unity 패키지에 직접 포함
- 버전 규칙: Core는 `MAJOR.MINOR`, Unity 패키지는 `MAJOR.MINOR.PATCH`

## 태스크
- [x] Unity 레포 루트에서 Core 서브모듈 추가 (`git submodule add https://github.com/arandra/GoogleSheetToData.git core`)
- [x] `Packages/com.arandra.gsheet-to-data/Editor` 구조 및 `package.json`, README, CHANGELOG, LICENSE 초안 작성
- [x] Core 저장소 내 기존 `GSheetToDataForUnity/Editor` 폴더를 정리하고(삭제) 새 Unity 레포 안내를 README에 명시 (Core/README.md 추가 완료)
- [x] Core 서브모듈 소스를 패키지 `Editor` asmdef에 편입하고 Google API DLL을 `Editor/Plugins/GoogleApis` 등으로 배치
- [x] `package.json`에 `com.unity.nuget.newtonsoft-json` 의존성을 명시하고 asmdef에서 precompiled references 설정
- [x] 사용자 설정 저장 방식을 ScriptableObject/EditorPrefs로 정리하여 민감정보가 버전 관리에 포함되지 않게 함
- [x] `Samples~` 폴더에 샘플 시나리오 추가, README에 OAuth/토큰/시트 설정 절차 문서화
- [ ] Unity LTS 프로젝트(결정 필요)에서 git URL 설치 테스트 및 CHANGELOG/README에 결과 기록 (Unity 2022.2 LTS 환경에서 검증 필요)
- [x] 버전 태그 생성 시 Core/Unity 버전 연동 규칙을 README에 명시

## 사전 결정/필요 사항
- [x] Unity 패키지 최소/권장 Unity 버전 : 2022.2 LTS
- [x] 포함할 Google API DLL 버전 세트 : 1.64.0 (Google.Apis/Auth/Core/Sheets/Drive)
- [x] 패키지에 적용할 라이선스 종류 결정 : MIT
- [x] README/샘플에서 사용할 예제 Sheet ID와 출력 경로 시나리오 선택 : (id : 1_2Y3BtltwsyXTovWuWV6J32x_Ebe2Sy8vybGNhzkIsM), (table 형: FieldTransform), (const 형 : InitConst )
- [x] 기본 OAuth 토큰 저장 경로 및 `client_secret.json` 배치 정책 결정 : OAuth 토큰은 unity project의 Temp 폴더 하위 `Temp/GSheetToData/` 에 저장.

## 문서화/Core
- [x] https://github.com/arandra/GoogleSheetToData.git 의 readme에 추가. 혹은 Document에 문서로 추가하고 링크. (Core/README.md 생성)
- [x] Sheet 작성요령. (table 방식과 const 방식 설명), (Document/SheetAuthoringGuide.md 추가)
- [x] Google cloud console에서 `client_secret.json` 생성하는 방법과 보안유의사항 설명. (Document/GoogleOAuthSetup.md 추가)
- [x] 각 하위 프로젝트의 링크 (예: GoogleSheetToDataForUnity) → Document/ProjectLinks.md 작성

## 문서화/Unity
- [x] https://github.com/arandra/GoogleSheetToDataForUnity.git 의 readme에 추가. 혹은 Document에 문서로 추가하고 링크.
- [x] 사용방법을 단계별로 설명. (Packages/com.arandra.gsheet-to-data/README.md 및 루트 README.md 갱신)
