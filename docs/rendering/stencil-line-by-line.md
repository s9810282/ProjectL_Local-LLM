**스텐실 합성 코드 — 한 줄씩 읽기**

현재 디스크의 StencillCustomBlending.compute 70줄과 StencilBlendingFeature.cs 270줄을 기준으로 작성했다. 각 코드 조각의 번호는 원본 줄 번호다. 빈 줄도 위치를 표시하고, 괄호는 블록의 시작과 끝으로 설명했다. 이 문서는 학습용이며 원본 실행 코드는 변경하지 않았다.

**읽을 때 사용할 이름**

A는 현재 카메라 화면, B는 TargetRenderTexture에 그린 화면이다. 깊이는 A 카메라의 깊이다. GPU는 화면 픽셀마다 같은 컴퓨트 함수를 실행한다. C#은 GPU에 입력을 연결하고 작업을 예약한다. 이 합성에서 스텐실 버퍼를 직접 읽지는 않는다.

**자료형과 기호**

- float: 소수 하나. float2/3/4: 소수 2/3/4개를 묶은 값.
- int: 부호 있는 정수. uint: 부호 없는 정수. bool: 참 또는 거짓.
- .xy/.xz/.rgb/.a: 묶인 값 중 필요한 성분만 선택한다.
- 벡터 +, -, *, /: 이 코드의 벡터 연산에서는 대응하는 성분끼리 계산한다. 행렬 곱셈은 별도 규칙을 따른다.
- =: 값을 대입. +=: 현재 값에 더함. *=: 현재 값에 곱함.
- &&: 모두 참. ||: 하나라도 참. !: 참/거짓 반전.
- 함수이름(인자): 값을 함수에 넣어 처리한다. 반환값이 있는 함수는 계산 결과를 돌려준다.

**컴퓨트 셰이더 전체**

[원본 StencillCustomBlending.compute](C:/Users/Hytean2-400/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Systems/Stencil/StencillCustomBlending.compute)

**1행**

```hlsl
#pragma kernel CSMain
```

Unity에 CSMain이라는 함수를 실행 가능한 커널로 등록한다. 커널은 C#에서 DispatchCompute로 실행할 GPU 작업의 시작 함수다.

*2행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**3행**

```hlsl
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
```

Unity 공통 HLSL 코드를 포함한다. 아래에서 호출하는 ComputeWorldSpacePosition과 플랫폼별 깊이·좌표 관련 정의를 여기서 가져온다.

*4행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**5행**

```hlsl
Texture2D<float4> _ActiveTex;
```

현재 카메라 화면 A를 읽는 2D 텍스처다. float4는 한 픽셀에서 R·G·B·A 네 값을 읽겠다는 뜻이다.

**6행**

```hlsl
Texture2D<float4> _TargetRenderTexture;
```

다른 카메라가 그린 화면 B를 읽는다. C#의 TargetRenderTexture가 연결된다.

**7행**

```hlsl
Texture2D<float> _DepthTex;
```

현재 카메라 깊이 텍스처다. 픽셀마다 깊이 한 값만 필요하므로 float를 사용한다. 이 값 자체가 월드 거리인 것은 아니다.

**8행**

```hlsl
Texture2D<float4> _LineNoiseTexture;
```

경계를 흔들기 위한 노이즈 텍스처다. 네 채널을 읽을 수 있지만 실제 계산은 R 채널만 사용한다.

**9행**

```hlsl
SamplerState sampler_LinearClamp;
```

LinearClamp 샘플러다. 텍스처 픽셀 사이를 읽으면 이웃 값을 선형 보간하고, UV가 0~1 밖이면 가장자리 값을 사용한다.

**10행**

```hlsl
SamplerState sampler_LinearRepeat;
```

LinearRepeat 샘플러다. 픽셀 사이를 보간하면서 텍스처 밖으로 나간 UV는 반복시킨다. UV 1.2는 반복 기준으로 0.2에 해당한다.

**11행**

```hlsl
RWTexture2D<float4> _Result;
```

GPU가 결과를 쓸 텍스처다. RW는 읽기/쓰기가 가능한 자원이라는 뜻이다. 현재 코드는 여기에 최종 색을 쓴다.

*12행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**13행**

```hlsl
uint _Width;
```

결과 화면의 가로 픽셀 수다. uint는 부호 없는 정수다.

**14행**

```hlsl
uint _Height;
```

결과 화면의 세로 픽셀 수다.

**15행**

```hlsl
int _WorldLineRatio; // World-space X of the boundary, not a screen percentage.
```

경계의 월드 X 좌표다. int이므로 정수 위치만 전달된다. 이름에 Ratio가 있지만 0~1 비율이 아니다.

**16행**

```hlsl
float _LineThickness; // Full width in world units.
```

선의 전체 폭이다. 월드 단위이며 뒤에서 절반으로 나눠 양쪽에 배치한다. 이 코드에서는 노이즈 강도로도 사용된다.

**17행**

```hlsl
float4 _LineColor;
```

선의 R·G·B·A다. RGB는 선 색상이고 A는 선을 얼마나 강하게 섞을지에 사용된다.

**18행**

```hlsl
float2 _LineNoiseOffset;
```

노이즈를 지금까지 얼마나 이동시켰는지 저장한 UV 오프셋이다. C#이 매 게임 프레임 누적해서 전달한다.

**19행**

```hlsl
float2 _LineNoiseTiling;
```

월드 X·Z 한 단위당 노이즈 반복 횟수다. float2의 x는 월드 X, y는 월드 Z에 대응한다.

**20행**

```hlsl
float4x4 _InverseViewProjection;
```

화면 위치와 깊이를 월드 위치로 되돌리는 4×4 역행렬이다. 카메라 위치·회전·정사영 Size 등의 정보가 들어 있다.

*21행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**22행**

```hlsl
[numthreads(8, 8, 1)]
```

스레드 그룹 하나를 8×8×1 = 64개의 GPU 작업으로 구성한다. 이 코드는 작업 하나가 픽셀 하나를 처리한다.

**23행**

```hlsl
void CSMain(uint3 id : SV_DispatchThreadID)
```

반환값이 없는 GPU 시작 함수다. uint3는 정수 3개짜리 자료형이다. SV_DispatchThreadID는 전체 실행 범위에서 각 작업의 고유 좌표를 GPU가 넣어주는 의미 표기다.

**24행**

```hlsl
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**25행**

```hlsl
if (id.x >= _Width || id.y >= _Height)
```

실행 좌표가 화면 가로나 세로 범위를 벗어났는지 확인한다. ||는 둘 중 하나라도 참이면 참이다. 그룹 수를 올림해서 실행하므로 끝에 초과 작업이 생길 수 있다.

**26행**

```hlsl
return;
```

범위 밖인 작업만 종료한다. 다른 픽셀의 작업까지 멈추는 명령은 아니다.

*27행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**28행**

```hlsl
float2 uv = (float2(id.xy) + 0.5) / float2(_Width, _Height);
```

id.xy를 소수 좌표로 바꾸고 0.5를 더해 픽셀 중앙을 잡은 다음, 가로·세로 크기로 각각 나눠 UV로 바꾼다. 벡터끼리의 나눗셈은 x/x, y/y로 수행된다. 가로 100픽셀의 첫 중앙은 0.5/100 = 0.005다.

**29행**

```hlsl
float4 source = _ActiveTex.Load(int3(id.xy, 0));
```

Load는 정수 텍셀 좌표에서 값을 직접 읽는 함수다. int3(id.xy, 0)의 앞 두 값은 픽셀 좌표이고 마지막 0은 mip 단계다. SampleLevel처럼 UV 보간을 하지 않는다. 화면 A의 RGBA를 source에 저장한다.

**30행**

```hlsl
float depth = _DepthTex.Load(int3(id.xy, 0));
```

같은 픽셀 위치의 깊이를 직접 읽는다. 이때 Load의 마지막 0 역시 mip 단계이며 깊이값 0을 지정하는 것이 아니다.

*31행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**32행**

```hlsl
// Sky has no surface to reconstruct or draw a terrain boundary on.
```

이 주석은 배경 깊이는 표면 복원에 사용하지 않겠다는 의도를 설명한다. 실제로는 아래의 깊이 끝값 비교로 판정한다.

**33행**

```hlsl
#if UNITY_REVERSED_Z
```

플랫폼이 역방향 깊이 방식을 사용하는 경우에만 다음 코드를 컴파일한다. #if는 GPU가 픽셀마다 실행하는 if와 달리 셰이더를 만들 때 갈라지는 분기다.

**34행**

```hlsl
bool hasSurface = depth > 0.000001;
```

역방향 깊이에서는 먼 끝/배경이 0 쪽이다. 0.000001보다 큰 값만 표면으로 취급한다. bool은 참/거짓이다. 정확히 배경만 식별하는 별도 태그가 아니므로 먼 끝에 매우 가까운 표면도 제외될 수 있다.

**35행**

```hlsl
#else
```

역방향 깊이를 쓰지 않는 플랫폼의 대체 코드로 넘어간다.

**36행**

```hlsl
bool hasSurface = depth < 0.999999;
```

일반 깊이에서는 먼 끝/배경이 1 쪽이다. 0.999999 미만인 값만 표면으로 취급한다.

**37행**

```hlsl
depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
```

lerp(a,b,t)는 a×(1-t)+b×t다. 텍스처 깊이 0~1을 플랫폼의 좌표 복원에 필요한 깊이 범위로 바꾼다. near 값이 -1이면 2×depth-1, near 값이 0이면 depth 그대로다. 월드 거리로 선형화하는 계산은 아니다.

**38행**

```hlsl
#endif
```

플랫폼별 깊이 분기를 끝낸다.

**39행**

```hlsl
if (!hasSurface)
```

!는 참/거짓을 뒤집는다. 따라서 hasSurface가 거짓일 때 다음 블록을 실행한다.

**40행**

```hlsl
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**41행**

```hlsl
_Result[id.xy] = source;
```

표면이 없는 픽셀에는 A의 원래 색을 그대로 쓴다. 이 경로에서는 B 화면도 섞지 않는다.

**42행**

```hlsl
return;
```

이 픽셀의 처리를 끝낸다. 뒤의 월드 복원·노이즈·합성 계산은 수행하지 않는다.

**43행**

```hlsl
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*44행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**45행**

```hlsl
float3 worldPos = ComputeWorldSpacePosition(uv, depth, _InverseViewProjection);
```

Unity의 ComputeWorldSpacePosition(화면UV, 기기깊이, 역VP행렬)을 호출한다. 함수 내부에서 UV를 투영 좌표로 바꾸고, 역행렬을 곱한 뒤 w로 나눠 월드 XYZ를 얻는다. 자세한 내부 계산은 문서 뒤에 풀이했다.

**46행**

```hlsl
// Tiling controls repeats per world unit. Gray is the neutral fallback.
```

타일링은 월드 단위당 반복 횟수라는 설명이다. 회색 기본 텍스처는 노이즈가 없는 상태를 의도한 대체 입력이다.

**47행**

```hlsl
// Subtract the offset so the visible pattern travels in the chosen direction.
```

UV에서 오프셋을 빼야 무늬가 지정한 양의 방향으로 이동한다는 설명이다. 텍스처를 읽는 위치와 화면에서 무늬가 움직이는 방향은 반대다.

**48행**

```hlsl
float2 noiseUV = worldPos.xz * _LineNoiseTiling - _LineNoiseOffset;
```

worldPos.xz는 XYZ 중 X와 Z만 뽑는다. 각 축에 타일링을 곱해 월드 좌표를 노이즈 UV로 바꾸고 누적 이동량을 뺀다. 타일링이 (0.1,0.1)이면 각 축 월드 10단위마다 한 번 반복한다.

**49행**

```hlsl
float noise = _LineNoiseTexture.SampleLevel(sampler_LinearRepeat, noiseUV, 0).r;
```

SampleLevel(샘플러, UV, mip단계)은 정규화된 UV에서 지정 mip를 읽는 함수다. 0은 원본 해상도 단계다. LinearRepeat로 보간·반복하고, 반환된 float4 중 .r로 빨간 채널 하나만 뽑는다.

**50행**

```hlsl
float signedDistance = worldPos.x - (float)_WorldLineRatio;
```

현재 표면의 월드 X에서 경계 X를 뺀다. (float)는 정수를 소수 자료형으로 바꾸는 형 변환이다. 경계가 3일 때 X=2면 -1, X=3이면 0, X=4면 +1이다.

**51행**

```hlsl
signedDistance += (noise - 0.5) * _LineThickness;
```

+=는 기존 값에 오른쪽 값을 더한다. 노이즈 0~1에서 0.5를 빼 -0.5~+0.5로 만든 뒤 선 두께를 곱한다. 두께 0.3이면 판정값을 -0.15~+0.15 흔든다. 이후 signedDistance는 노이즈로 변형된 경계 판정값이며 정확한 최단거리는 아니다.

*52행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**53행**

```hlsl
// Approximate pixel footprint without derivatives (compute shader).
```

화면 미분 대신 주변 UV 복원으로 픽셀 폭을 근사한다는 주석이다. 현재 구현은 옆 픽셀의 실제 깊이와 노이즈 변화량을 사용하지 않는다.

**54행**

```hlsl
float3 worldRight = ComputeWorldSpacePosition(
```

오른쪽으로 화면 한 픽셀 이동했을 때의 가상 월드 위치를 구하는 함수 호출을 시작한다.

**55행**

```hlsl
uv + float2(1.0 / _Width, 0), depth, _InverseViewProjection);
```

UV의 가로에 1/너비를 더한다. depth는 옆 픽셀에서 새로 읽지 않고 현재 값을 재사용한다. 따라서 실제 옆 표면이 아닌, 같은 깊이 평면 위의 위치다.

**56행**

```hlsl
float3 worldUp = ComputeWorldSpacePosition(
```

화면 UV 세로 방향으로 한 픽셀 이동한 가상 월드 위치를 구한다.

**57행**

```hlsl
uv + float2(0, 1.0 / _Height), depth, _InverseViewProjection);
```

UV의 세로에 1/높이를 더한다. 여기서도 현재 픽셀 깊이를 재사용한다. worldUp이라는 이름은 월드 Y 방향을 뜻하지 않는다.

**58행**

```hlsl
float feather = max(abs(worldRight.x - worldPos.x) + abs(worldUp.x - worldPos.x), 0.0001);
```

abs는 절댓값이다. 한 픽셀 가로 이동과 세로 이동에 따른 월드 X 변화 크기를 더한다. max(a,b)는 큰 쪽을 고르므로 폭이 최소 0.0001이 되게 한다. 이 폭이 feather이며, 경계를 부드럽게 섞는 구간에 사용한다.

**59행**

```hlsl
float targetMask = smoothstep(-feather * 0.5, feather * 0.5, signedDistance);
```

smoothstep(a,b,x)는 x≤a이면 0, x≥b이면 1, 중간이면 부드러운 0~1을 반환한다. 경계 판정값이 -feather/2에서 +feather/2로 넘어가는 구간에서 B의 비율을 늘린다. 경계 중앙 값 0에서는 0.5다.

*60행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**61행**

```hlsl
// UV sampling supports a B RenderTexture with a different resolution.
```

B 화면은 픽셀 번호 대신 UV로 읽어 해상도가 달라도 대응되는 화면 비율 위치를 읽을 수 있다는 주석이다. 두 카메라의 시점·투영·종횡비를 맞추는 일까지 자동으로 해주지는 않는다.

**62행**

```hlsl
float4 target = _TargetRenderTexture.SampleLevel(sampler_LinearClamp, uv, 0);
```

B 텍스처에서 같은 화면 UV의 RGBA를 읽는다. LinearClamp로 보간하고 바깥 UV는 가장자리로 제한한다. 마지막 0은 mip 단계다.

**63행**

```hlsl
float4 color = lerp(source, target, targetMask);
```

lerp는 성분별로 작동한다. source×(1-targetMask)+target×targetMask로 RGBA를 섞는다. 마스크가 0이면 A, 1이면 B, 0.5면 반씩이다.

**64행**

```hlsl
float lineMask = 1.0 - smoothstep(
```

선 표시 비율 lineMask 계산을 시작한다. smoothstep 결과를 1에서 빼므로 경계 가까이는 1, 멀어질수록 0이 되게 만든다.

**65행**

```hlsl
max(0.0, _LineThickness * 0.5 - feather * 0.5),
```

smoothstep의 시작값 a다. 전체 두께의 절반에서 부드러운 폭의 절반을 뺀다. max(0,...)로 음수가 되지 않게 한다. 경계까지의 값이 a 이하인 곳은 선 비율 1이다.

**66행**

```hlsl
_LineThickness * 0.5 + feather * 0.5, abs(signedDistance));
```

첫 식은 smoothstep의 끝값 b다. 두께/2+feather/2 이상 떨어지면 선 비율이 0이다. 마지막 인자 abs(signedDistance)는 어느 쪽에 있는지 부호를 없애 경계 양쪽을 동일하게 처리한다.

**67행**

```hlsl
lineMask *= step(0.000001, _LineThickness) * saturate(_LineColor.a);
```

step(edge,x)는 x<edge이면 0, 아니면 1이다. 두께가 0.000001 미만이면 선을 끈다. saturate(x)는 값을 0~1로 제한하므로 색상 알파를 선의 불투명도 비율로 사용한다. *=는 기존 lineMask에 이 두 값을 곱한다.

**68행**

```hlsl
color.rgb = lerp(color.rgb, _LineColor.rgb, lineMask);
```

RGB만 원래 합성 색과 선 색 사이에서 섞는다. lineMask=1이면 선 RGB, 0이면 합성 RGB다. color.a는 수정하지 않는다. 주변 조명을 다시 계산하는 함수는 아니다.

**69행**

```hlsl
_Result[id.xy] = color;
```

이 GPU 작업이 맡은 픽셀에 최종 RGBA를 기록한다.

**70행**

```hlsl
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**ComputeWorldSpacePosition 내부도 따라가기**

이 함수는 프로젝트에 설치된 Unity Core 패키지의 [Common.hlsl](C:/Users/Hytean2-400/Project_L/Library/PackageCache/com.unity.render-pipelines.core@d7b87cecdab8/ShaderLibrary/Common.hlsl:1358)에 있다. 이름만 외우지 말고 아래 세 단계를 보면 된다.

```hlsl
float4 positionCS = ComputeClipSpacePosition(positionNDC, deviceDepth);
```

여기서 positionNDC 인자는 앞에서 만든 uv다. ComputeClipSpacePosition은 UV의 가로·세로를 각각 `uv * 2 - 1`로 바꾼다. 0→-1, 0.5→0, 1→1이다. 깊이를 z에 넣고 w에는 1을 넣는다. 필요한 플랫폼에서는 y 부호도 뒤집는다. C#이 선택한 GPU 투영 규약과 함께 맞춰 쓰는 변환이므로 임의로 Y 반전을 추가하면 안 된다.

```hlsl
float4 hpositionWS = mul(invViewProjMatrix, positionCS);
```

mul(행렬, 벡터)는 행렬 변환을 적용하는 HLSL 함수다. 여기서는 카메라 투영을 거꾸로 되돌린다. 일반 벡터끼리 성분별로 곱하는 것과 다르다. 결과는 XYZ 외에 W가 포함된 동차 좌표다.

```hlsl
return hpositionWS.xyz / hpositionWS.w;
```

XYZ를 각각 W로 나눠 실제 월드 좌표로 바꾼다. 예를 들어 변환 결과가 (4,6,8,2)이면 실제 위치는 (2,3,4)다. W는 픽셀 깊이가 아니라 투영 좌표를 표현하는 데 쓰는 추가 성분이다. 표준 정사영에서는 역변환 결과 W가 보통 1이고, 같은 공식으로 처리할 수 있다.

**smoothstep 안의 계산**

```hlsl
float t = saturate((x - a) / (b - a));
float result = t * t * (3.0 - 2.0 * t);
```

a<b일 때의 식이다. 첫 줄은 x가 a~b 사이의 어느 지점인지 0~1로 바꾸고 범위를 제한한다. 둘째 줄은 시작과 끝의 변화가 완만하도록 곡선을 만든다. t가 0, 0.5, 1일 때 결과도 0, 0.5, 1이다. t=0.25에서는 0.15625, t=0.75에서는 0.84375다. 단순 lerp의 직선 진행과 이 중간 변화가 다르다.

예를 들어 feather=0.02이면 targetMask는 signedDistance=-0.01에서 0, 0에서 0.5, +0.01에서 1이다. 이후 lerp가 그 비율로 A/B를 섞는다.

**선 계산을 숫자로 대입하기**

노이즈가 0.5, 경계 X=3, 선 전체 두께=0.3, feather=0.02라고 하자.

```text
signedDistance = worldX - 3
선이 완전히 칠해지는 한쪽 범위 = 0.3/2 - 0.02/2 = 0.14
선이 완전히 사라지는 한쪽 범위 = 0.3/2 + 0.02/2 = 0.16
```

worldX=3이면 abs(signedDistance)=0이므로 lineMask는 1이다. worldX=3.15이면 거리 0.15가 0.14~0.16의 중앙이므로 lineMask는 0.5다. worldX=3.2이면 거리 0.2가 바깥이므로 lineMask는 0이다. 선 색상 알파가 0.5이면 각각에 0.5를 곱해서 실제 혼합 비율은 0.5, 0.25, 0이 된다.

노이즈가 1이면 판정값에 +0.15가 더해져 경계 중심은 X=2.85로 이동한다. 노이즈가 0이면 -0.15가 더해져 중심은 X=3.15로 이동한다. 단, 노이즈가 위치마다 달라지므로 이 설명은 각 지점의 노이즈 값을 고정해서 본 것이다. 전체 모양에 대한 정확한 최단거리 계산은 아니다.

**C# 렌더 기능 전체**

[원본 StencilBlendingFeature.cs](C:/Users/Hytean2-400/Project_L/Assets/ProjectL/Script/Render/RendererFeatures/Systems/Stencil/StencilBlendingFeature.cs)

호출 순서는 공통 BaseRenderFeature의 Create → CreatePass → 생성자, 카메라 렌더링 시 PreparePass → 패스 등록 → RecordRenderGraph → 그래프 실행 때 Execute다. 종료/재생성 때 Dispose로 래퍼를 정리한다. 아래는 원본 파일 배치 순서대로 설명한다.

**1행**

```csharp
using ProjectL.Render.Shader.PostProcessing.Core;
```

프로젝트의 공통 렌더 기능 네임스페이스를 사용한다. BaseRenderFeature 등의 이름을 짧게 쓸 수 있게 한다. using은 이 줄에서 렌더링을 실행하는 명령이 아니다.

**2행**

```csharp
using UnityEngine;
```

Color, Vector2, ComputeShader, RenderTexture 같은 Unity 기본 타입을 가져온다.

**3행**

```csharp
using UnityEngine.Rendering;
```

RTHandle, RenderTargetIdentifier 등 렌더링 관련 타입을 가져온다.

**4행**

```csharp
using UnityEngine.Rendering.RenderGraphModule;
```

RenderGraph, TextureHandle, AccessFlags 등 렌더 그래프 타입을 가져온다.

**5행**

```csharp
using UnityEngine.Rendering.RenderGraphModule.Util;
```

AddBlitPass 같은 RenderGraph 유틸리티 확장 함수를 사용할 수 있게 한다.

**6행**

```csharp
using UnityEngine.Rendering.Universal;
```

ScriptableRenderPass, UniversalCameraData 등 URP 타입을 가져온다.

*7행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**8행**

```csharp
namespace ProjectL.Render.Shader.PostProcessing.Stencil
```

이 파일에서 정의하는 클래스들의 소속 이름 공간을 선언한다. 같은 클래스 이름끼리 충돌하는 것을 줄인다.

**9행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

*10행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**11행**

```csharp
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Render.Shader.PostProcessing.Stencil", sourceAssembly: "Assembly-CSharp", sourceClassName: "StencilBlending")]
```

MovedFrom 속성은 이전 클래스 이름·어셈블리 정보를 Unity에 알려준다. 클래스 이름/소속 변경 후 직렬화된 참조를 이전하는 데 쓰며 화면 계산식에는 참여하지 않는다.

**12행**

```csharp
public class StencilBlendingFeature : BaseRenderFeature<StencilBlendingFeature.StencilBlendingPass>
```

StencilBlendingFeature 클래스를 선언한다. :는 상속이며, 프로젝트의 BaseRenderFeature에 내부 StencilBlendingPass 타입을 지정한다. 공통 베이스가 생성·등록·해제 흐름을 관리한다.

**13행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**14행**

```csharp
[System.Serializable]
```

Serializable은 아래 설정 클래스의 필드를 Unity가 저장하고 Inspector에 표시할 수 있게 하는 속성이다.

**15행**

```csharp
public class StencilSetting
```

효과 설정을 묶은 클래스다. public은 다른 코드에서도 접근할 수 있다는 뜻이다.

**16행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**17행**

```csharp
public RenderTexture TargetRenderTexture;
```

합성할 B 화면을 담은 RenderTexture 참조다. RenderTexture는 카메라/GPU가 그림을 기록할 수 있는 텍스처다.

**18행**

```csharp
[Tooltip("Boundary world X coordinate. Smaller X uses A; larger X uses B.")]
```

Tooltip은 Inspector에 마우스를 올렸을 때 보여줄 설명이다. 작은 월드 X는 A, 큰 X는 B라는 의미다.

**19행**

```csharp
public int WorldLineRatio;
```

경계 월드 X 좌표를 정수로 저장한다. 초기값을 안 적은 int 필드의 기본값은 0이다.

**20행**

```csharp
[Tooltip("Full line width in world units.")]
```

선 두께가 월드 단위의 전체 폭이라는 Inspector 설명이다.

**21행**

```csharp
[Min(0f)] public float LineThickness = 0.1f;
```

두께 기본값은 0.1이다. float는 소수를 저장하고 0.1f의 f는 float 상수 표시다. Min은 Inspector 입력의 하한을 지정하지만 외부 코드의 대입까지 막지는 않는다.

**22행**

```csharp
[ColorUsage(true, true)] public Color LineColor = Color.white;
```

선 색상 기본값은 흰색이다. ColorUsage의 첫 true는 알파 입력, 둘째 true는 HDR 색상 입력을 허용한다. HDR은 RGB에 1보다 큰 밝기도 입력할 수 있다는 뜻이다.

**23행**

```csharp
public Texture2D LineNoiseTexture;
```

경계를 흔들 때 사용할 노이즈 이미지 참조다. 지정하지 않으면 null이다.

**24행**

```csharp
[Tooltip("Noise repeats per world unit (X = world X, Y = world Z).")]
```

타일링 값은 월드 단위당 반복 횟수이며 Vector2의 두 축을 월드 X/Z로 쓴다는 설명이다.

**25행**

```csharp
public Vector2 LineNoiseTiling = Vector2.one;
```

Vector2.one은 (1,1)이다. 기본적으로 X/Z 각 월드 1단위마다 노이즈를 반복한다.

**26행**

```csharp
[Tooltip("Noise flow direction on the world XZ plane (X = world X, Y = world Z).")]
```

노이즈 이동 방향을 월드 XZ 평면 기준으로 입력한다는 설명이다. 실제 이동량은 타일링과 함께 해석해야 한다.

**27행**

```csharp
public Vector2 LineNoiseDirection = Vector2.up;
```

Vector2.up은 (0,1)이다. 이 코드의 두 번째 성분은 월드 Z에 대응한다. 이후 normalized로 방향 길이를 1로 맞춘다.

**28행**

```csharp
[Tooltip("Noise repeats per second. Zero pauses; negative values reverse the flow.")]
```

속도는 노이즈 UV 이동량/초로 사용한다. 0이면 정지, 음수면 반대 방향이라는 설명이다. 실제 월드 이동 속도는 축별 타일링에 영향을 받는다.

**29행**

```csharp
public float LineNoiseSpeed = 0.2f;
```

노이즈 UV의 기본 이동 속도를 초당 0.2로 설정한다.

**30행**

```csharp
[Tooltip("Requires a CSMain kernel.")]
```

연결할 컴퓨트 셰이더에 CSMain 커널이 있어야 한다는 Inspector 설명이다.

**31행**

```csharp
public ComputeShader BlendingShader;
```

실제 GPU 계산을 수행할 ComputeShader 자산 참조다.

**32행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*33행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**34행**

```csharp
public class StencilBlendingPass : ScriptableRenderPass, System.IDisposable
```

URP 렌더 패스 클래스를 선언한다. ScriptableRenderPass를 상속하고 IDisposable을 구현한다. IDisposable은 사용을 끝낼 때 Dispose로 자원을 정리하겠다는 약속이다.

**35행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**36행**

```csharp
public class PassData
```

그래프 기록 단계에서 실행 단계로 넘길 데이터를 묶는 클래스다. RenderGraph가 이 타입의 인스턴스를 생성해 준다.

**37행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**38행**

```csharp
public TextureHandle Source, Target, Depth, Noise, Result;
```

Source=A, Target=B, Depth=현재 깊이, Noise=노이즈, Result=출력이다. TextureHandle은 픽셀 배열 자체가 아니라 해당 RenderGraph 안에서 텍스처를 가리키는 식별자다.

**39행**

```csharp
public ComputeShader Shader;
```

실행할 ComputeShader 참조를 저장한다.

**40행**

```csharp
public int Kernel, Width, Height, GroupsX, GroupsY;
```

실행 커널 번호, 화면 가로/세로, 가로/세로 스레드 그룹 수를 저장한다. 그룹 수와 픽셀 수는 서로 다르다.

**41행**

```csharp
public int WorldLineRatio;
```

이번 패스에서 사용할 경계의 월드 X 좌표다.

**42행**

```csharp
public float LineThickness;
```

이번 패스의 전체 선 두께다.

**43행**

```csharp
public Color LineColor;
```

이번 패스의 선 색상이다.

**44행**

```csharp
public Vector2 LineNoiseOffset;
```

이번 패스에서 사용할 누적 노이즈 UV 이동량이다.

**45행**

```csharp
public Vector2 LineNoiseTiling;
```

이번 패스의 노이즈 반복 횟수다.

**46행**

```csharp
public Matrix4x4 InverseViewProjection;
```

이번 카메라의 화면+깊이를 월드 좌표로 되돌리는 4×4 행렬이다.

**47행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*48행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**49행**

```csharp
private RenderTexture _target;
```

현재 RTHandle로 감싸 둔 대상 RenderTexture를 기억한다. private은 이 클래스 내부에서만 접근한다는 뜻이다.

**50행**

```csharp
private Texture2D _noise;
```

현재 RTHandle로 감싸 둔 노이즈 텍스처를 기억한다.

**51행**

```csharp
private RTHandle _targetHandle, _noiseHandle;
```

외부 텍스처를 URP 자원 관리 방식으로 다룰 래퍼다. 아래에서 기존 텍스처를 감싸는 용도로 만들며 B 화면을 새로 그리는 함수가 아니다.

**52행**

```csharp
public const string KernelName = "CSMain";
```

커널 이름을 문자열 상수 CSMain으로 정의한다. const이므로 실행 중 다른 값으로 바꿀 수 없다.

**53행**

```csharp
private readonly ComputeShader _shader;
```

생성자에서 정한 셰이더 참조다. readonly는 생성자 이후 다른 참조를 대입하지 못하게 한다.

**54행**

```csharp
private readonly int _kernel = -1;
```

커널 번호 기본값 -1은 아직 유효한 커널을 찾지 못했다는 내부 표식이다.

**55행**

```csharp
private readonly uint _threadsX, _threadsY;
```

그룹 하나에 포함되는 가로/세로 작업 수다. 현재 컴퓨트 셰이더에서는 각각 8을 읽는다.

**56행**

```csharp
private Vector2 _noiseOffset;
```

지금까지 누적한 노이즈 이동량이다. Vector2 기본값은 (0,0)이다.

**57행**

```csharp
private int _lastNoiseFrame = -1;
```

이 패스 인스턴스가 마지막으로 노이즈를 갱신한 프레임 번호다. -1에서 시작한다.

*58행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**59행**

```csharp
public bool IsReady => _shader != null && _shader == _setting.BlendingShader &&
```

IsReady는 읽을 때 계산되는 bool 프로퍼티다. => 오른쪽 식이 결과다. 셰이더가 존재하고 설정의 셰이더와 같은지 검사한다. &&는 모든 조건이 참이어야 참이며 앞이 거짓이면 뒤 평가를 생략한다.

**60행**

```csharp
_kernel >= 0 && _threadsX > 0 && _threadsY > 0;
```

커널 번호가 유효하고 그룹의 가로·세로 작업 수가 양수인지도 검사한다. 여기서는 장치 지원 여부나 텍스처까지 검사하지 않는다.

*61행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**62행**

```csharp
public readonly StencilSetting _setting;
```

설정 객체 참조를 저장한다. readonly는 참조 교체를 제한하지만 객체 안의 WorldLineRatio 같은 필드 값 변경은 허용한다.

*63행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**64행**

```csharp
public StencilBlendingPass(StencilSetting setting)
```

생성자다. new StencilBlendingPass(setting)로 객체를 만들 때 실행되며 반환형을 쓰지 않는다.

**65행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**66행**

```csharp
_setting = setting;
```

생성자로 전달받은 설정 객체를 보관한다. 설정 내용을 새 객체로 복사하는 것이 아니라 같은 객체를 참조한다.

**67행**

```csharp
requiresIntermediateTexture = true;
```

이 패스는 화면 결과를 텍스처로 읽어야 하므로 URP에 중간 카메라 컬러 텍스처가 필요하다고 알린다.

**68행**

```csharp
ConfigureInput(ScriptableRenderPassInput.Depth);
```

ConfigureInput은 패스가 필요로 하는 카메라 입력을 URP에 요청한다. 여기서는 Depth, 즉 카메라 깊이 텍스처를 준비해 달라는 뜻이다. 직접 깊이를 읽는 함수는 아니다.

**69행**

```csharp
_shader = setting.BlendingShader;
```

설정에 연결된 컴퓨트 셰이더를 보관한다.

**70행**

```csharp
if (_shader != null && _shader.HasKernel(KernelName))
```

셰이더가 있고 HasKernel("CSMain")이 참인지 검사한다. HasKernel은 해당 이름의 실행 시작 함수가 존재하는지 확인한다.

**71행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**72행**

```csharp
_kernel = _shader.FindKernel(KernelName);
```

FindKernel은 커널 이름에 대응하는 정수 번호를 반환한다. 나중에 바인딩과 DispatchCompute에서 이 번호를 쓴다.

**73행**

```csharp
_shader.GetKernelThreadGroupSizes(_kernel, out uint threadsX, out uint threadsY, out _);
```

GetKernelThreadGroupSizes는 numthreads의 X/Y/Z 값을 읽는다. out은 함수가 결과를 변수에 써준다는 뜻이다. 마지막 out _는 Z 결과를 사용하지 않고 버린다.

**74행**

```csharp
_threadsX = threadsX;
```

읽은 그룹 X 크기를 저장한다.

**75행**

```csharp
_threadsY = threadsY;
```

읽은 그룹 Y 크기를 저장한다.

**76행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**77행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*78행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**79행**

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

URP가 호출하는 RenderGraph 기록 함수다. override는 부모의 가상 함수를 이 구현으로 바꾼다는 뜻이다. 여기서는 작업과 자원 관계를 등록하며, GPU 계산은 나중에 실행된다.

**80행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**81행**

```csharp
UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
```

frameData.Get<T>()는 이번 카메라 렌더링 문맥에서 T 타입의 데이터를 가져온다. UniversalResourceData에는 현재 컬러/깊이 텍스처 등의 핸들이 있다.

**82행**

```csharp
UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
```

이번 카메라의 행렬·설정 등 UniversalCameraData를 가져온다.

*83행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**84행**

```csharp
if (resourceData.isActiveTargetBackBuffer || !IsReady || _setting.TargetRenderTexture == null)
```

현재 출력이 직접 표시용 백버퍼이거나, 패스 준비가 안 됐거나, B 텍스처가 없으면 건너뛴다. ||는 하나라도 참이면 참, !IsReady는 준비가 안 됐다는 뜻이다.

**85행**

```csharp
return;
```

현재 RecordRenderGraph 호출을 끝낸다. 이번 카메라에는 이 합성 작업을 등록하지 않는다.

*86행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**87행**

```csharp
TextureHandle source = resourceData.activeColorTexture;
```

현재 활성 카메라 컬러 텍스처를 A 화면으로 사용한다.

**88행**

```csharp
TextureHandle depth = resourceData.cameraDepthTexture;
```

URP가 준비한 카메라 깊이 텍스처 핸들을 가져온다.

**89행**

```csharp
if (!source.IsValid() || !depth.IsValid()) return;
```

IsValid()는 사용할 수 있는 그래프 핸들인지 확인한다. 컬러 또는 깊이 핸들이 유효하지 않으면 즉시 종료한다. 텍스처 내용이 맞는지까지 검증하는 함수는 아니다.

*90행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**91행**

```csharp
// Integrate once per game frame: speed changes do not jump the UV phase,
```

매 프레임 한 번씩 속도를 적분하겠다는 주석이다. 시간×현재 속도로 처음부터 계산하는 대신 작은 이동량을 누적하면 속도 변경 때 위치가 갑자기 점프하지 않는다.

**92행**

```csharp
// and additional cameras cannot advance the animation twice.
```

같은 패스 인스턴스를 여러 카메라가 사용해도 같은 프레임에 두 번 이동시키지 않겠다는 설명이다.

**93행**

```csharp
if (Application.isPlaying && _lastNoiseFrame != Time.frameCount)
```

게임 실행 중이며, 이번 프레임 번호가 마지막 갱신 번호와 다를 때만 이동량을 갱신한다. 편집 모드에서는 이 애니메이션을 진행하지 않는다.

**94행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**95행**

```csharp
Vector2 velocity = _setting.LineNoiseDirection.normalized * _setting.LineNoiseSpeed;
```

normalized는 방향 벡터의 길이를 1로 만든다(영벡터는 0). 예: (3,4)는 (0.6,0.8). 여기에 속도를 곱해 초당 UV 이동 벡터 velocity를 만든다.

**96행**

```csharp
_noiseOffset += velocity * Time.deltaTime;
```

Time.deltaTime은 프레임 간 시간 간격(초)이다. 속도×시간을 기존 오프셋에 더한다. 초당 0.2, 프레임 간격 0.02초면 이번에 0.004만큼 움직인다.

**97행**

```csharp
_noiseOffset.x = Mathf.Repeat(_noiseOffset.x, 1f);
```

Mathf.Repeat(value, length)는 값을 반복 구간으로 감싼다. 여기서는 UV 가로 성분을 1 주기로 감싸 큰 값이 계속 누적되는 것을 막는다. 예: 1.2는 0.2. 반복 텍스처이므로 정수 주기 차이는 같은 무늬다.

**98행**

```csharp
_noiseOffset.y = Mathf.Repeat(_noiseOffset.y, 1f);
```

노이즈 UV 세로 성분도 1 주기로 감싼다.

**99행**

```csharp
_lastNoiseFrame = Time.frameCount;
```

이번 프레임에 이동량을 갱신했다고 기록한다.

**100행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*101행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**102행**

```csharp
if (_target != _setting.TargetRenderTexture || _targetHandle == null)
```

설정에서 B 텍스처가 교체되었거나 아직 래퍼가 없으면 다음 블록에서 래퍼를 준비한다.

**103행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**104행**

```csharp
_targetHandle?.Release();
```

?.는 null이 아닐 때만 호출하는 연산자다. Release는 이전 RTHandle 래퍼를 해제한다. 이 코드처럼 외부 식별자로 감싼 대상 RenderTexture 자산 자체를 Destroy하는 명령이 아니다.

**105행**

```csharp
_target = _setting.TargetRenderTexture;
```

새 B 텍스처 참조를 기억한다.

**106행**

```csharp
// Import only the color view: a camera RT also has a depth attachment.
```

카메라 RT에는 깊이 버퍼도 있으므로, 이 패스에는 색상 뷰만 가져오려는 의도를 설명한 주석이다.

**107행**

```csharp
_targetHandle = RTHandles.Alloc(new RenderTargetIdentifier(_target));
```

RenderTargetIdentifier는 기존 렌더 타깃을 지정하는 값이다. RTHandles.Alloc의 이 오버로드는 그 식별자를 RTHandle로 감싼다. 이름이 Alloc이어도 여기서 새 B 화면을 렌더링하거나 픽셀을 복사하지 않는다.

**108행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*109행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**110행**

```csharp
Texture2D noise = _setting.LineNoiseTexture != null
```

설정의 노이즈 텍스처가 존재하는지 검사하며 아래 줄의 삼항 연산자와 이어진다.

**111행**

```csharp
? _setting.LineNoiseTexture : Texture2D.grayTexture;
```

조건 ? 참일 때 값 : 거짓일 때 값 형식이다. 노이즈가 있으면 그 텍스처, 없으면 Unity의 기본 회색 텍스처를 선택한다.

**112행**

```csharp
if (_noise != noise || _noiseHandle == null)
```

선택된 노이즈가 바뀌었거나 래퍼가 없을 때 다음 블록을 실행한다.

**113행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**114행**

```csharp
_noiseHandle?.Release();
```

기존 노이즈 RTHandle 래퍼가 있다면 해제한다.

**115행**

```csharp
_noise = noise;
```

이번에 사용할 노이즈 텍스처를 기억한다.

**116행**

```csharp
_noiseHandle = RTHandles.Alloc(_noise);
```

기존 Texture2D를 RTHandle로 감싼다. 원본 이미지 내용을 생성하거나 변경하지 않는다.

**117행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*118행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**119행**

```csharp
var descriptor = renderGraph.GetTextureDesc(source);
```

GetTextureDesc는 A 텍스처의 크기·포맷 등 생성 명세를 가져온다. var는 컴파일러가 오른쪽 결과로 자료형을 추론하게 한다. 픽셀 내용을 읽는 함수가 아니다.

**120행**

```csharp
descriptor.name = "StencilBlendingResult";
```

새 출력 텍스처에 디버깅용 이름을 붙인다.

**121행**

```csharp
descriptor.depthBufferBits = DepthBits.None;
```

합성 결과는 색상만 저장하므로 출력 텍스처에 깊이 버퍼를 붙이지 않도록 한다.

**122행**

```csharp
descriptor.msaaSamples = MSAASamples.None;
```

출력 텍스처를 MSAA 없는 단일 샘플 텍스처로 설정한다. 컴퓨트의 RWTexture2D 출력에 맞추기 위한 설정이다.

**123행**

```csharp
descriptor.enableRandomWrite = true;
```

GPU 컴퓨트 셰이더가 임의 픽셀 위치에 쓸 수 있도록 출력 자원의 쓰기 기능을 켠다. 랜덤 노이즈를 생성한다는 뜻은 아니다.

**124행**

```csharp
TextureHandle result = renderGraph.CreateTexture(descriptor);
```

CreateTexture는 이 명세의 그래프 관리 텍스처를 등록하고 TextureHandle을 반환한다. 실제 자원 확보와 수명은 RenderGraph가 관리한다.

**125행**

```csharp
TextureHandle target = renderGraph.ImportTexture(_targetHandle, new RenderTargetInfo
```

ImportTexture는 그래프 밖에서 만든 텍스처를 이번 그래프에서 사용하도록 등록한다. 새 텍스처로 복사하는 함수가 아니다. RenderTargetInfo로 외부 대상의 정보를 명시한다.

**126행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**127행**

```csharp
width = _target.width,
```

외부 B 텍스처의 가로 픽셀 수를 전달한다.

**128행**

```csharp
height = _target.height,
```

외부 B 텍스처의 세로 픽셀 수를 전달한다.

**129행**

```csharp
volumeDepth = _target.volumeDepth,
```

외부 텍스처의 슬라이스/깊이 수를 전달한다. 일반적인 2D 텍스처에서는 1이다.

**130행**

```csharp
msaaSamples = _target.antiAliasing,
```

외부 B 텍스처의 MSAA 샘플 수를 전달한다. 이 필드에 값을 적는다고 B가 단일 샘플로 변환되지는 않는다.

**131행**

```csharp
format = _target.graphicsFormat,
```

외부 B 텍스처의 그래픽 포맷을 전달한다. 채널 수와 정밀도 같은 정보다.

**132행**

```csharp
bindMS = _target.bindTextureMS
```

외부 RT가 멀티샘플 텍스처로 직접 바인딩되는지에 대한 설정을 전달한다.

**133행**

```csharp
});
```

RenderTargetInfo 초기화와 ImportTexture 함수 호출을 끝내고 target에 핸들을 받는다.

**134행**

```csharp
TextureHandle noiseTexture = renderGraph.ImportTexture(_noiseHandle);
```

노이즈 텍스처도 이번 RenderGraph의 입력 자원으로 등록한다.

*135행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**136행**

```csharp
using (var builder = renderGraph.AddComputePass<PassData>("StencilBlendingPass", out PassData passData))
```

AddComputePass<PassData>는 컴퓨트 작업을 그래프에 추가하고 설정용 builder와 out passData를 준다. using은 블록 끝에서 builder.Dispose를 호출해 등록 작업을 마무리한다. 이 자리에서 픽셀 계산을 바로 수행하는 것은 아니다.

**137행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**138행**

```csharp
passData.Source = source;
```

실행 단계에서 A 텍스처를 찾도록 핸들을 저장한다.

**139행**

```csharp
passData.Target = target;
```

실행 단계에서 B 텍스처를 찾도록 핸들을 저장한다.

**140행**

```csharp
passData.Depth = depth;
```

깊이 텍스처 핸들을 저장한다.

**141행**

```csharp
passData.Noise = noiseTexture;
```

노이즈 텍스처 핸들을 저장한다.

**142행**

```csharp
passData.Result = result;
```

출력 텍스처 핸들을 저장한다.

**143행**

```csharp
passData.Shader = _shader;
```

실행할 셰이더 참조를 저장한다.

**144행**

```csharp
passData.Kernel = _kernel;
```

실행할 커널 번호를 저장한다.

**145행**

```csharp
passData.Width = descriptor.width;
```

출력의 가로 픽셀 수를 저장한다.

**146행**

```csharp
passData.Height = descriptor.height;
```

출력의 세로 픽셀 수를 저장한다.

**147행**

```csharp
passData.GroupsX = Mathf.CeilToInt(descriptor.width / (float)_threadsX);
```

가로 픽셀 수÷그룹 가로 작업 수를 소수 나눗셈으로 계산하고 CeilToInt로 올림해 정수로 만든다. 예: 1921/8=240.125 → 241그룹. 나머지 픽셀도 처리하려고 올림한다.

**148행**

```csharp
passData.GroupsY = Mathf.CeilToInt(descriptor.height / (float)_threadsY);
```

세로 그룹 수도 동일하게 계산한다. 예: 1080/8=135그룹. 초과 작업은 컴퓨트의 범위 검사에서 종료된다.

**149행**

```csharp
passData.WorldLineRatio = _setting.WorldLineRatio;
```

경계 위치의 현재 설정값을 이번 패스 데이터에 복사한다.

**150행**

```csharp
passData.LineThickness = Mathf.Max(0f, _setting.LineThickness);
```

Mathf.Max는 큰 값을 선택한다. 음수 두께가 코드에서 들어와도 0 이상으로 제한해 전달한다.

**151행**

```csharp
passData.LineNoiseOffset = _noiseOffset;
```

이번 프레임까지 누적한 노이즈 UV 이동량을 저장한다.

**152행**

```csharp
passData.LineNoiseTiling = _setting.LineNoiseTiling;
```

현재 노이즈 타일링을 저장한다.

**153행**

```csharp
passData.LineColor = QualitySettings.activeColorSpace == ColorSpace.Linear
```

프로젝트의 실제 색상 공간이 Linear인지 검사한다. 아래 줄과 함께 삼항 연산자를 구성한다.

**154행**

```csharp
? _setting.LineColor.linear : _setting.LineColor;
```

Linear라면 Color.linear로 RGB를 감마 표현에서 선형 표현으로 변환한 값을 전달하고, 아니면 원래 색을 전달한다. 알파는 이 변환 대상이 아니다.

**155행**

```csharp
// RecordRenderGraph cannot query the legacy cameraColorTargetHandle.
```

RenderGraph 기록 중에는 이전 방식의 cameraColorTargetHandle 조회를 피해야 한다는 기존 주석이다.

**156행**

```csharp
// This pass requires an intermediate texture and skips the backbuffer,
```

이 패스가 중간 텍스처를 요구하고 백버퍼 경로를 건너뛴다는 전제를 설명한다.

**157행**

```csharp
// so explicitly use the render-to-texture GPU projection convention.
```

그 전제하에 텍스처에 렌더링하는 GPU 투영 규약을 명시해서 사용한다는 설명이다.

**158행**

```csharp
Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(
```

GL.GetGPUProjectionMatrix는 Unity의 투영행렬을 현재 그래픽 API의 깊이 범위·렌더 타깃 방향 규약에 맞춘 GPU용 행렬로 바꾼다.

**159행**

```csharp
cameraData.GetProjectionMatrix(), true);
```

GetProjectionMatrix는 이번 카메라의 투영행렬을 가져온다. 두 번째 인자 true는 렌더 텍스처에 그리는 경우의 규약을 선택한다. 원근 카메라로 바꾸라는 뜻이 아니다.

**160행**

```csharp
passData.InverseViewProjection =
```

PassData의 역 View-Projection 행렬을 채우는 대입식의 시작이다.

**161행**

```csharp
(gpuProjection * cameraData.GetViewMatrix()).inverse;
```

View는 월드→카메라 좌표, Projection은 카메라→투영 좌표다. 열벡터에 적용할 때 P×V는 V부터 적용된다. .inverse는 합성 변환을 되돌리는 행렬이다. 정사영 Size도 Projection에 포함된다.

*162행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**163행**

```csharp
builder.UseTexture(source, AccessFlags.Read);
```

UseTexture는 이 패스가 source를 읽는다고 그래프에 선언한다. 그래프가 실행 순서와 자원 수명을 계산하는 데 사용한다. 셰이더 변수 연결은 Execute에서 별도로 한다.

**164행**

```csharp
builder.UseTexture(target, AccessFlags.Read);
```

B 텍스처를 읽는 의존성을 선언한다.

**165행**

```csharp
builder.UseTexture(depth, AccessFlags.Read);
```

깊이 텍스처를 읽는 의존성을 선언한다.

**166행**

```csharp
builder.UseTexture(noiseTexture, AccessFlags.Read);
```

노이즈 텍스처를 읽는 의존성을 선언한다.

**167행**

```csharp
builder.UseTexture(result, AccessFlags.Write);
```

출력 텍스처에 쓰는 의존성을 선언한다.

**168행**

```csharp
builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) => Execute(context, data));
```

SetRenderFunc는 그래프가 실행될 때 호출할 함수를 등록한다. 람다의 data는 위에서 채운 PassData, context는 실행 명령 문맥이다. static은 바깥 인스턴스/지역 변수를 캡처하지 않게 한다. 실제 함수 본문은 Execute(context,data)다.

**169행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*170행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**171행**

```csharp
renderGraph.AddBlitPass(result, source, Vector2.one, Vector2.zero,
```

AddBlitPass는 result를 source로 복사하는 후속 그래프 패스를 등록한다. Vector2.one=(1,1)은 UV 배율을 유지하고 Vector2.zero=(0,0)는 UV 이동을 하지 않는다는 뜻이다.

**172행**

```csharp
passName: "Stencil Blending Copy");
```

복사 패스의 디버깅 이름을 지정하고 호출을 끝낸다.

**173행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*174행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**175행**

```csharp
private static void Execute(ComputeGraphContext context, PassData data)
```

그래프 실행 시 호출할 함수다. static이므로 특정 패스 객체의 this 없이 실행된다. data에 기록 단계에서 저장한 입력이 들어 있다.

**176행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**177행**

```csharp
context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_ActiveTex", data.Source);
```

SetComputeTextureParam(셰이더, 커널번호, 변수이름, 텍스처)은 해당 커널의 텍스처 변수에 자원을 연결하는 명령을 기록한다. 여기서는 _ActiveTex에 A 화면을 연결한다.

**178행**

```csharp
context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_TargetRenderTexture", data.Target);
```

_TargetRenderTexture에 B 화면을 연결한다.

**179행**

```csharp
context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_DepthTex", data.Depth);
```

_DepthTex에 현재 카메라 깊이를 연결한다.

**180행**

```csharp
context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_LineNoiseTexture", data.Noise);
```

_LineNoiseTexture에 노이즈 텍스처를 연결한다.

**181행**

```csharp
context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Result", data.Result);
```

_Result에 GPU가 써야 할 출력 텍스처를 연결한다.

**182행**

```csharp
context.cmd.SetComputeIntParam(data.Shader, "_Width", data.Width);
```

SetComputeIntParam은 셰이더의 정수 변수에 값을 전달하는 명령이다. _Width에 화면 너비를 전달한다.

**183행**

```csharp
context.cmd.SetComputeIntParam(data.Shader, "_Height", data.Height);
```

_Height에 화면 높이를 전달한다.

**184행**

```csharp
context.cmd.SetComputeIntParam(data.Shader, "_WorldLineRatio", data.WorldLineRatio);
```

_WorldLineRatio에 정수 경계 X 좌표를 전달한다. 이후 float로 바꿔도 이 단계에서 없던 소수 좌표가 생기지는 않는다.

**185행**

```csharp
context.cmd.SetComputeFloatParam(data.Shader, "_LineThickness", data.LineThickness);
```

SetComputeFloatParam은 float 값 전달 함수다. _LineThickness에 전체 선 두께를 전달한다.

**186행**

```csharp
context.cmd.SetComputeVectorParam(data.Shader, "_LineColor", data.LineColor);
```

SetComputeVectorParam은 4성분 벡터를 전달한다. Color의 RGBA가 Vector4의 XYZW에 대응해 _LineColor로 전달된다.

**187행**

```csharp
context.cmd.SetComputeVectorParam(data.Shader, "_LineNoiseOffset",
```

노이즈 이동량을 벡터로 전달하는 호출을 시작한다.

**188행**

```csharp
new Vector4(data.LineNoiseOffset.x, data.LineNoiseOffset.y, 0f, 0f));
```

Vector2의 X/Y를 Vector4의 앞 두 값에 넣고 뒤 두 값은 0으로 채운다. 셰이더의 float2는 앞 두 성분을 사용한다.

**189행**

```csharp
context.cmd.SetComputeVectorParam(data.Shader, "_LineNoiseTiling",
```

노이즈 타일링을 벡터로 전달하는 호출을 시작한다.

**190행**

```csharp
new Vector4(data.LineNoiseTiling.x, data.LineNoiseTiling.y, 0f, 0f));
```

타일링 X/Y를 4성분 벡터의 앞부분에 넣고 나머지는 0으로 채운다.

**191행**

```csharp
context.cmd.SetComputeMatrixParam(data.Shader, "_InverseViewProjection", data.InverseViewProjection);
```

SetComputeMatrixParam은 4×4 행렬을 셰이더 변수에 전달한다. _InverseViewProjection은 월드 좌표 복원에 쓰인다.

**192행**

```csharp
context.cmd.DispatchCompute(data.Shader, data.Kernel, data.GroupsX, data.GroupsY, 1);
```

DispatchCompute는 지정 커널을 그룹 수 X/Y/Z만큼 실행하는 GPU 명령을 기록한다. 픽셀 수가 아니라 그룹 수를 넣는다. 현재 각 그룹이 8×8×1이므로 전체 작업 수는 GroupsX×GroupsY×64다.

**193행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*194행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**195행**

```csharp
public void Dispose()
```

IDisposable의 자원 정리 함수다. 이 프로젝트의 공통 BaseRenderFeature가 패스 교체/해제 시 호출한다. 매 프레임 호출하는 함수가 아니다.

**196행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**197행**

```csharp
_targetHandle?.Release();
```

대상 텍스처용 RTHandle 래퍼가 있으면 해제한다.

**198행**

```csharp
_noiseHandle?.Release();
```

노이즈용 RTHandle 래퍼가 있으면 해제한다.

**199행**

```csharp
_targetHandle = null;
```

해제한 대상 핸들 참조를 null로 비운다.

**200행**

```csharp
_noiseHandle = null;
```

해제한 노이즈 핸들 참조도 비운다.

**201행**

```csharp
_target = null;
```

기억하던 대상 텍스처 참조를 비운다. 이 대입이 원본 RenderTexture 자산을 삭제하지는 않는다.

**202행**

```csharp
_noise = null;
```

기억하던 노이즈 텍스처 참조를 비운다.

**203행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**204행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*205행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**206행**

```csharp
public override RenderFeatureType Type => RenderFeatureType.StencilBlending;
```

공통 렌더 관리 코드에 이 기능의 종류가 StencilBlending임을 알려주는 프로퍼티다.

**207행**

```csharp
protected override RenderPassEvent InjectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;
```

패스 등록 시점을 URP 후처리 직전으로 지정한다. 화면 합성 결과에 이후 메인 카메라 후처리가 적용된다.

*208행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**209행**

```csharp
public StencilSetting setting = new StencilSetting();
```

Inspector에서 편집할 설정 객체를 생성한다. new는 새 클래스 인스턴스를 만든다.

*210행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**211행**

```csharp
protected override StencilBlendingPass CreatePass() => new StencilBlendingPass(setting);
```

CreatePass가 호출되면 현재 설정을 참조하는 새 StencilBlendingPass를 만들어 반환한다. =>는 짧은 함수 본문 표기다.

*212행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**213행**

```csharp
protected override bool PreparePass(StencilBlendingPass pass, ref RenderingData renderingData)
```

공통 베이스가 등록 전에 호출하는 조건 검사 함수다. ref는 RenderingData를 참조로 전달받는다는 뜻이다. bool 반환값이 참이어야 패스를 등록한다.

**214행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**215행**

```csharp
return SystemInfo.supportsComputeShaders && pass.IsReady &&
```

장치가 컴퓨트 셰이더를 지원하고 패스도 준비되어 있어야 한다.

**216행**

```csharp
renderingData.cameraData.cameraType == CameraType.Game &&
```

카메라 종류가 Game이어야 한다. 따라서 이 구현은 SceneView 카메라에 합성을 등록하지 않는다.

**217행**

```csharp
renderingData.cameraData.renderType == CameraRenderType.Base &&
```

카메라 스택의 Base 카메라여야 한다. Overlay 카메라에는 이 패스를 추가하지 않는다.

**218행**

```csharp
!renderingData.cameraData.xr.enabled && setting.TargetRenderTexture != null &&
```

XR이 비활성화되어 있고 대상 B 텍스처가 있어야 한다.

**219행**

```csharp
renderingData.cameraData.camera.targetTexture != setting.TargetRenderTexture;
```

현재 카메라가 B 텍스처를 직접 그리는 카메라여서는 안 된다. B 카메라가 자기 결과를 다시 B와 합성하는 경로를 제외한다. 지금까지의 모든 && 조건이 참이면 true를 반환한다.

**220행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*221행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**222행**

```csharp
public enum Property
```

외부에서 바꿀 수 있는 설정 항목 이름을 enum(열거형)으로 선언한다.

**223행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**224행**

```csharp
WorldLineRatio,
```

경계 월드 X 좌표 설정 키다.

**225행**

```csharp
LineThickness,
```

선 전체 두께 설정 키다.

**226행**

```csharp
LineColor,
```

선 색상 설정 키다.

**227행**

```csharp
LineNoiseTexture,
```

노이즈 텍스처 설정 키다.

**228행**

```csharp
LineNoiseTiling,
```

노이즈 반복 횟수 설정 키다.

**229행**

```csharp
LineNoiseDirection,
```

노이즈 이동 방향 설정 키다.

**230행**

```csharp
LineNoiseSpeed,
```

노이즈 이동 속도 설정 키다.

**231행**

```csharp
TargetRenderTexture,
```

B 화면 텍스처 설정 키다.

**232행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

*233행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**234행**

```csharp
public override void SetProperty<TKey, TValue>(TKey key, TValue value)
```

외부 코드가 설정을 변경하는 함수다. TKey/TValue는 전달 타입을 받는 제네릭 매개변수다. 제네릭이라고 모든 조합을 허용하는 것은 아니며 아래에서 실제 타입을 검사한다.

**235행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**236행**

```csharp
if (key is not Property property)
```

key가 이 클래스의 Property enum인지 검사한다. 맞으면 property라는 변수로 다룰 수 있고, 아니면 다음 줄의 예외를 던진다.

**237행**

```csharp
throw new System.ArgumentException("Invalid property key for this renderer feature.", nameof(key));
```

ArgumentException은 잘못된 인자가 전달되었다는 오류다. throw는 현재 정상 실행을 중단하고 예외를 전달한다. nameof(key)는 변수 이름을 문자열 "key"로 얻는다.

*238행: 읽기 편하게 나눈 빈 줄이다. 실행되는 명령은 없다.*

**239행**

```csharp
switch (property)
```

property 값에 따라 맞는 case 블록 하나를 선택한다.

**240행**

```csharp
{
```

바로 앞에서 선언한 클래스·함수·조건문 등의 블록을 시작한다. 이 괄호 자체가 수학 계산을 하지는 않는다.

**241행**

```csharp
case Property.WorldLineRatio when value is int value0:
```

키가 WorldLineRatio이고 value가 int일 때만 실행한다. when은 추가 조건, value is int value0는 타입 확인과 값 추출이다.

**242행**

```csharp
setting.WorldLineRatio = value0;
```

추출한 정수를 경계 좌표에 대입한다.

**243행**

```csharp
break;
```

switch 분기에서 빠져나간다. break는 여기서 함수 전체를 종료하는 return과는 다르다.

**244행**

```csharp
case Property.LineThickness when value is float value1:
```

LineThickness 키와 float 값 조합인지 검사한다.

**245행**

```csharp
setting.LineThickness = value1;
```

선 두께를 갱신한다. GPU에 즉시 보내는 것이 아니라 다음 그래프 기록에서 설정을 읽는다.

**246행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**247행**

```csharp
case Property.LineColor when value is Color value2:
```

LineColor 키와 Color 값 조합인지 검사한다.

**248행**

```csharp
setting.LineColor = value2;
```

선 색상을 갱신한다.

**249행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**250행**

```csharp
case Property.LineNoiseTexture when value is Texture2D value3:
```

LineNoiseTexture 키와 Texture2D 객체 조합인지 검사한다. null은 이 타입 패턴에 맞지 않으므로 이 API로 null을 전달하면 default로 간다.

**251행**

```csharp
setting.LineNoiseTexture = value3;
```

노이즈 텍스처 참조를 갱신한다.

**252행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**253행**

```csharp
case Property.LineNoiseTiling when value is Vector2 value4:
```

LineNoiseTiling 키와 Vector2 값 조합인지 검사한다.

**254행**

```csharp
setting.LineNoiseTiling = value4;
```

노이즈 타일링을 갱신한다.

**255행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**256행**

```csharp
case Property.LineNoiseDirection when value is Vector2 value5:
```

LineNoiseDirection 키와 Vector2 값 조합인지 검사한다.

**257행**

```csharp
setting.LineNoiseDirection = value5;
```

노이즈 방향을 갱신한다. 길이 정규화는 프레임 갱신 계산에서 한다.

**258행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**259행**

```csharp
case Property.LineNoiseSpeed when value is float value6:
```

LineNoiseSpeed 키와 float 값 조합인지 검사한다.

**260행**

```csharp
setting.LineNoiseSpeed = value6;
```

노이즈 속도를 갱신한다. 누적 위치 자체는 초기화하지 않는다.

**261행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**262행**

```csharp
case Property.TargetRenderTexture when value is RenderTexture value7:
```

TargetRenderTexture 키와 RenderTexture 객체 조합인지 검사한다. null은 이 패턴에도 맞지 않는다.

**263행**

```csharp
setting.TargetRenderTexture = value7;
```

합성할 B 텍스처 참조를 갱신한다. 다음 기록 시 기존 참조와 비교해 RTHandle을 교체한다.

**264행**

```csharp
break;
```

현재 switch 분기를 끝낸다.

**265행**

```csharp
default:
```

어느 case의 키·타입 조건에도 맞지 않았을 때 실행한다.

**266행**

```csharp
throw new System.ArgumentException("Property value has an unsupported type.", nameof(value));
```

지원하지 않는 값 타입이라는 ArgumentException을 던진다. 예를 들어 두께에 float 1f 대신 int 1을 전달하면 이 경로로 온다.

**267행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**268행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**269행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**270행**

```csharp
}
```

현재 블록을 끝낸다. 클래스·함수·조건문 중 무엇을 닫는지는 들여쓰기와 앞의 여는 괄호에 대응한다.

**두 파일의 연결 예**

```csharp
context.cmd.SetComputeFloatParam(data.Shader, "_LineThickness", data.LineThickness);
```

이 명령으로 넘긴 값이 컴퓨트의 `float _LineThickness;`에 들어간다. 인자 속 문자열은 셰이더 변수 이름과 정확히 일치해야 한다. SetComputeTextureParam/IntParam/VectorParam/MatrixParam도 각각 같은 역할을 텍스처/정수/벡터/행렬에 수행한다.

```text
화면 1920×1080
커널 numthreads = 8×8×1
GroupsX = ceil(1920/8) = 240
GroupsY = ceil(1080/8) = 135
전체 작업 = 240×135×64 = 2,073,600
화면 픽셀 = 1920×1080 = 2,073,600
```

화면 크기가 8의 배수가 아니면 초과 작업이 생기고 컴퓨트 25~26행의 범위 검사로 제외된다.

**이 코드의 동작 범위**

깊이는 A 화면 기준이며 B의 깊이를 비교하지 않는다. 깊이에 표면이 없는 픽셀은 A를 그대로 유지한다. B는 같은 UV로 읽으므로 카메라 시점·투영이 서로 맞지 않으면 같은 물체 위치가 합쳐지지 않는다. 선 두께와 노이즈 흔들림 강도는 현재 한 변수에 연결되어 있다. feather는 같은 깊이에서 한 픽셀의 월드 X 폭을 구하는 근사이며 표면의 깊이 변화와 노이즈 기울기까지 반영하지 않는다.

정사영 Size는 C#에서 얻은 투영행렬에 포함되어 있다. 이 계산에 Size를 추가로 곱하는 방식은 중복 적용이 될 수 있다. 이 설명은 현재 소스와 설치된 패키지를 읽은 결과이며 실제 화면의 정합성을 실행 검증한 결과는 아니다.

