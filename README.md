# IFPC

Instant and fast processing of commands.

## 사용법

이거 라이브러리임, c# 프로그램에 추가해서 쓰면 됨.

### 실행

```cs
using IFPC;

Processer p = new();

p.Run("code.ifpc");
```

알잘딱깔센

### 명령 추가

```cs
Processer p = new();

p.AddCommand("printf",(d) => {
	Console.WriteLine(d.value);
	return null;
});
````

## IFPC 언어

### 변수
#### 선언
변수 선언에 타입 지정 없음
파이썬 처럼 그냥 하면 됨 

```
myvariable = 614
```

#### 삭제

```
myvariable =
```

### 함수

#### 선언
< 로 함수를 열고 > 로 함수를 닫음.

이런 형식임.
```
< (함수이름) / (파라미터)
	(코드)
>
```

예시를 들자면
```
< functionname / parameter1 parameter2
	#t1 = type parameter1
	#t2 = type parameter2

	if #t1 is not "number" / return null
	if #t2 is not "number" / return null

	result = #t1 + #t2
	return result
>
```

참고로 함수 안에서 함수를 또 선언할수 없음.

#### 호출
평범한 명령어와 다름없이 그냥 이름 쓰면 됨. 그리고 띄어쓰기로 인자를 하나씩 넣어주면 됨.
```
result = functionname 614 5020
```
#### 제거

```
functionname =
```
변수랑 똑같음

### 문

#### 레이블
곧 후술할 'goto' 랑 'skip' 명령어에 필요한것.

```
:(이름)
```
그냥 이렇게 선언하면 됨.
중복선언 가능.

#### 이동 (goto)
 'goto (레이블)' 을 입력하면 됨.
```
goto (레이블 명)
```

예시:
```
P:

console.write "무한반복"

goto P
```

함수 코드 안에 있는 레이블은 무시함.

레이블은 중복 선언이 가능하기에
이미 지나쳤던 레이블로 가능경우 가장 최근에 접했던 레이블로 이동.
지나쳤던 레이블이 없는경우 곧 후술할 'skip' 명령어처럼 아레로 레이블을 계속 찾으러 감.

만약 레이블이 없으면 코드 실행을 취소함.

#### 건너뛰기 (skip)
goto랑 기능 자체는 비슷하지만, 조금 다양하게 사용 가능함.

goto를 사용할때처럼 레이블을 만들고
```
skip (레이블 명)
```
을 하면 됨. 하지만 goto와 다른점은 코드 위쪽으로 이동하지 못함.

즉, 'skip'  명령어를 사용한 열부터 코드가 끝나는 열까지 계속 레이블을 찾아서 이동함.
(함수 코드 안에 있는 레이블을 무시하지 않음.)

즉 존재하지 않는 레이블을 입력하면 계속 레이블을 찾다가 못찾고 프로그램이 종료되게 할수 있음.

#### 만약 (if)
```


if 
```