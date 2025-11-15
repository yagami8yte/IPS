# Forte Credentials 확인 방법

## 방법 1: Dex 포털에서 직접 확인

1. **Forte Dex 로그인**:
   - Sandbox: https://sandbox.forte.net/dex/
   - 계정이 없으면: https://www.forte.net/test-account-setup

2. **API Credentials 확인**:
   - 로그인 후 메뉴: **Developer > API Credentials**
   - 현재 사용 중인 credentials:
     ```
     API Access ID: 03a04fee3e438b44ef168052227cf9ac
     API Secure Key: c24eadad0838c40a8bb469c67d71eceb (마지막 생성 시 저장한 값)
     ```
   - ⚠️ **주의**: API Secure Key는 생성 시에만 표시되고 다시 볼 수 없습니다!

3. **Location ID 확인**:
   - 메뉴: **Settings > Locations**
   - 현재 Location ID: `411494`

## 방법 2: 간단한 curl 테스트 (REST API v3 이용)

Forte REST API v3를 사용해서 credentials가 유효한지 확인:

```bash
# Windows PowerShell
$apiAccessId = "03a04fee3e438b44ef168052227cf9ac"
$apiSecureKey = "c24eadad0838c40a8bb469c67d71eceb"
$orgId = "507890"

$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${apiAccessId}:${apiSecureKey}"))

Invoke-WebRequest -Uri "https://sandbox.forte.net/api/v3/organizations/${orgId}/locations" `
  -Headers @{
    "Authorization" = "Basic $auth"
    "X-Forte-Auth-Organization-Id" = $orgId
  } `
  -Method GET
```

```bash
# Linux/Mac (curl)
curl -X GET "https://sandbox.forte.net/api/v3/organizations/507890/locations" \
  -H "Authorization: Basic $(echo -n '03a04fee3e438b44ef168052227cf9ac:c24eadad0838c40a8bb469c67d71eceb' | base64)" \
  -H "X-Forte-Auth-Organization-Id: 507890"
```

### 성공 응답 (HTTP 200):
```json
{
  "response": {
    "code": 200,
    "message": "Success"
  },
  "locations": [...]
}
```

### 실패 응답 (HTTP 401):
```json
{
  "response": {
    "code": 401,
    "message": "Unauthorized"
  }
}
```

## 방법 3: Organization ID로 credentials 매칭 확인

appsettings.json의 값들:
- API Access ID: `03a04fee3e438b44ef168052227cf9ac`
- API Secure Key: `c24eadad0838c40a8bb469c67d71eceb`
- Organization ID: `507890`
- Location ID: `411494`

**확인할 것**:
1. 이 credentials가 Organization `507890`에 속해 있는가?
2. Location `411494`가 이 Organization에 속해 있는가?

Dex에서 확인:
- Developer > API Credentials에서 API Access ID 확인
- Settings > Locations에서 Location ID 확인
- 둘 다 같은 Organization에 속해야 함

## 방법 4: Forte Checkout v2 실제 테스트

브라우저에서 `test_forte_credentials.html` 열고:
1. F12 개발자 도구 열기
2. Console 탭 확인
3. "Test Credentials" 버튼 클릭
4. 에러 메시지 확인:
   - "Invalid authentication" = credentials 잘못됨
   - "Checkout loaded" = credentials 올바름

## 일반적인 문제

### 문제 1: Sandbox vs Production 혼동
- ✅ Sandbox credentials는 `sandbox.forte.net`에서만 작동
- ❌ Production credentials는 sandbox에서 작동 안 함

### 문제 2: API Secure Key 분실
- API Secure Key는 생성 시에만 표시됩니다
- 분실 시 Dex에서 재생성 필요 (Developer > API Credentials > Regenerate)

### 문제 3: Organization/Location 불일치
- API credentials가 Organization A에 속하는데
- Location ID가 Organization B에 속하면 인증 실패

## 추천 순서

1. **먼저 Dex 포털 로그인**해서 credentials 직접 확인
2. **PowerShell/curl 테스트**로 REST API 접근 가능 여부 확인
3. **HTML 테스트 페이지**로 Checkout v2 작동 확인

---

## 현재 상황 진단

`Invalid authentication` 에러가 나는 이유는 99% 다음 중 하나:

1. ❌ API Access ID 틀림
2. ❌ API Secure Key 틀림
3. ❌ Location ID가 해당 Organization에 속하지 않음
4. ❌ Sandbox credentials인데 production 모드로 시도
5. ❌ Production credentials인데 sandbox 모드로 시도

→ **Dex 포털에 로그인해서 실제 값과 비교 필수!**
